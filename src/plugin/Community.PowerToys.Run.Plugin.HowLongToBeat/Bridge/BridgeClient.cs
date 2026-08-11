using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

public sealed class BridgeClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = false,
        };

    private readonly string _executablePath;
    private readonly TimeSpan _requestTimeout;

    private readonly SemaphoreSlim _startLock =
        new(1, 1);

    private readonly SemaphoreSlim _writeLock =
        new(1, 1);

    private readonly ConcurrentDictionary<
        long,
        TaskCompletionSource<JsonElement>>
        _pending = new();

    private Process? _process;

    private Task? _stdoutTask;
    private Task? _stderrTask;

    private long _nextRequestId;
    private bool _disposed;

    public BridgeClient(
        string executablePath,
        TimeSpan? requestTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            executablePath);

        _executablePath =
            Path.GetFullPath(executablePath);

        _requestTimeout =
            requestTimeout ?? TimeSpan.FromSeconds(10);
    }

    public static BridgeClient CreateDefault()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Bridge",
            "hltb-bridge.exe");

        return new BridgeClient(path);
    }

    public Task<BridgePingResult> PingAsync(
        CancellationToken cancellationToken = default)
    {
        return SendRequestAsync<BridgePingResult>(
            "ping",
            new { },
            cancellationToken);
    }

    public Task<BridgeSearchResult> SearchAsync(
        string query,
        BridgeSearchMode mode = BridgeSearchMode.All,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        return SendRequestAsync<BridgeSearchResult>(
            "search",
            new
            {
                query,
                mode = mode.ToProtocolValue(),
            },
            cancellationToken);
    }

    public async Task<BridgeGame?> GetByIdAsync(
        int gameId,
        CancellationToken cancellationToken = default)
    {
        if (gameId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gameId));
        }

        var response =
            await SendRequestAsync<BridgeGameLookupResult>(
                "getById",
                new
                {
                    gameId,
                },
                cancellationToken);

        return response.Game;
    }

    public async Task ShutdownAsync(
        CancellationToken cancellationToken = default)
    {
        var process = _process;

        if (process is null || HasExited(process))
        {
            return;
        }

        try
        {
            await SendRequestAsync<BridgeShutdownResult>(
                "shutdown",
                new { },
                cancellationToken);
        }
        catch
        {
            // Shutdown is best-effort.
        }

        process = _process;

        if (process is null || HasExited(process))
        {
            return;
        }

        using var timeout =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken);

        timeout.CancelAfter(
            TimeSpan.FromSeconds(2));

        try
        {
            await process.WaitForExitAsync(
                timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
        }
    }

    private async Task<T> SendRequestAsync<T>(
        string method,
        object parameters,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        await EnsureStartedAsync(
            cancellationToken);

        var requestId =
            Interlocked.Increment(
                ref _nextRequestId);

        var completion =
            new TaskCompletionSource<JsonElement>(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

        if (!_pending.TryAdd(
                requestId,
                completion))
        {
            throw new BridgeException(
                "internal_error",
                "Failed to register bridge request.");
        }

        var payload = JsonSerializer.Serialize(
            new
            {
                id = requestId,
                method,
                @params = parameters,
            },
            JsonOptions);

        try
        {
            await WriteLineAsync(
                payload,
                cancellationToken);
        }
        catch (Exception exc)
        {
            _pending.TryRemove(
                requestId,
                out _);

            throw new BridgeException(
                "write_error",
                "Failed to write to the HLTB bridge.",
                exc);
        }

        using var timeout =
            new CancellationTokenSource(
                _requestTimeout);

        using var linked =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken,
                    timeout.Token);

        try
        {
            var result =
                await completion.Task.WaitAsync(
                    linked.Token);

            var deserialized =
                result.Deserialize<T>(
                    JsonOptions);

            if (deserialized is null)
            {
                throw new BridgeException(
                    "protocol_error",
                    "Bridge returned an empty result.");
            }

            return deserialized;
        }
        catch (OperationCanceledException)
            when (
                timeout.IsCancellationRequested
                && !cancellationToken
                    .IsCancellationRequested)
        {
            _pending.TryRemove(
                requestId,
                out _);

            await TryCancelRemoteAsync(
                requestId);

            throw new BridgeException(
                "timeout",
                $"Bridge request timed out after " +
                $"{_requestTimeout.TotalSeconds:0.#} seconds.");
        }
        catch (OperationCanceledException)
        {
            _pending.TryRemove(
                requestId,
                out _);

            await TryCancelRemoteAsync(
                requestId);

            throw;
        }
    }

    private async Task EnsureStartedAsync(
        CancellationToken cancellationToken)
    {
        var current = _process;

        if (current is not null &&
            !HasExited(current))
        {
            return;
        }

        await _startLock.WaitAsync(
            cancellationToken);

        try
        {
            current = _process;

            if (current is not null &&
                !HasExited(current))
            {
                return;
            }

            if (!File.Exists(_executablePath))
            {
                throw new BridgeException(
                    "bridge_not_found",
                    "HLTB bridge executable was not found: " +
                    _executablePath);
            }

            var workingDirectory =
                Path.GetDirectoryName(
                    _executablePath);

            if (string.IsNullOrWhiteSpace(
                    workingDirectory))
            {
                throw new BridgeException(
                    "bridge_path_error",
                    "Could not determine bridge directory.");
            }

            var startInfo =
                new ProcessStartInfo
                {
                    FileName = _executablePath,
                    WorkingDirectory =
                        workingDirectory,

                    UseShellExecute = false,

                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    CreateNoWindow = true,

                    StandardOutputEncoding =
                        Encoding.UTF8,

                    StandardErrorEncoding =
                        Encoding.UTF8,
                };

            var process =
                Process.Start(startInfo);

            if (process is null)
            {
                throw new BridgeException(
                    "bridge_start_error",
                    "Failed to start HLTB bridge.");
            }

            _process = process;

            _stdoutTask =
                ReadStdoutAsync(process);

            _stderrTask =
                ReadStderrAsync(process);
        }
        finally
        {
            _startLock.Release();
        }
    }

    private async Task WriteLineAsync(
        string line,
        CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(
            cancellationToken);

        try
        {
            var process = _process;

            if (process is null ||
                HasExited(process))
            {
                throw new BridgeException(
                    "bridge_exited",
                    "HLTB bridge is not running.");
            }

            await process.StandardInput
                .WriteLineAsync(line);

            await process.StandardInput
                .FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ReadStdoutAsync(
        Process process)
    {
        try
        {
            while (true)
            {
                var line =
                    await process.StandardOutput
                        .ReadLineAsync();

                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                HandleResponseLine(
                    process,
                    line);
            }
        }
        catch (Exception exc)
        {
            if (ReferenceEquals(
                    _process,
                    process))
            {
                FailPending(
                    new BridgeException(
                        "bridge_read_error",
                        "Failed reading bridge output.",
                        exc));
            }
        }
        finally
        {
            if (ReferenceEquals(
                    _process,
                    process))
            {
                FailPending(
                    new BridgeException(
                        "bridge_exited",
                        "HLTB bridge process exited."));
            }
        }
    }

    private void HandleResponseLine(
        Process process,
        string line)
    {
        try
        {
            using var document =
                JsonDocument.Parse(line);

            var root =
                document.RootElement;

            if (!root.TryGetProperty(
                    "id",
                    out var idElement) ||
                idElement.ValueKind !=
                    JsonValueKind.Number ||
                !idElement.TryGetInt64(
                    out var requestId))
            {
                // Responses with id=null are protocol
                // errors for requests we did not originate.
                return;
            }

            if (!_pending.TryRemove(
                    requestId,
                    out var completion))
            {
                // Usually a late response to a
                // cancelled request.
                return;
            }

            if (!root.TryGetProperty(
                    "ok",
                    out var okElement) ||
                okElement.ValueKind is not
                    JsonValueKind.True and
                    not JsonValueKind.False)
            {
                completion.TrySetException(
                    new BridgeException(
                        "protocol_error",
                        "Bridge response is missing 'ok'."));

                return;
            }

            if (okElement.GetBoolean())
            {
                if (!root.TryGetProperty(
                        "result",
                        out var resultElement))
                {
                    completion.TrySetException(
                        new BridgeException(
                            "protocol_error",
                            "Bridge response is missing 'result'."));

                    return;
                }

                completion.TrySetResult(
                    resultElement.Clone());

                return;
            }

            var code =
                "bridge_error";

            var message =
                "HLTB bridge returned an error.";

            if (root.TryGetProperty(
                    "error",
                    out var errorElement) &&
                errorElement.ValueKind ==
                    JsonValueKind.Object)
            {
                if (errorElement.TryGetProperty(
                        "code",
                        out var codeElement) &&
                    codeElement.ValueKind ==
                        JsonValueKind.String)
                {
                    code =
                        codeElement.GetString()
                        ?? code;
                }

                if (errorElement.TryGetProperty(
                        "message",
                        out var messageElement) &&
                    messageElement.ValueKind ==
                        JsonValueKind.String)
                {
                    message =
                        messageElement.GetString()
                        ?? message;
                }
            }

            completion.TrySetException(
                new BridgeException(
                    code,
                    message));
        }
        catch (JsonException exc)
        {
            if (!ReferenceEquals(
                    _process,
                    process))
            {
                return;
            }

            var error =
                new BridgeException(
                    "protocol_error",
                    "Bridge wrote invalid JSON to stdout.",
                    exc);

            FailPending(error);

            TryKill(process);
        }
    }

    private static async Task ReadStderrAsync(
    Process process)
    {
        try
        {
            while (true)
            {
                var line =
                    await process.StandardError
                        .ReadLineAsync();

                if (line is null)
                {
                    break;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    Debug.WriteLine(
                        $"[HLTB bridge] {line}");
                }
            }
        }
        catch
        {
            // stderr is diagnostic-only.
        }
    }

    private async Task TryCancelRemoteAsync(
        long targetRequestId)
    {
        var process = _process;

        if (process is null ||
            HasExited(process))
        {
            return;
        }

        var controlId =
            Interlocked.Increment(
                ref _nextRequestId);

        var payload =
            JsonSerializer.Serialize(
                new
                {
                    id = controlId,
                    method = "cancel",
                    @params = new
                    {
                        requestId =
                            targetRequestId,
                    },
                },
                JsonOptions);

        try
        {
            await WriteLineAsync(
                payload,
                CancellationToken.None);
        }
        catch
        {
            // Cancellation is best-effort.
        }
    }

    private void FailPending(
        Exception exception)
    {
        foreach (var item in _pending)
        {
            if (_pending.TryRemove(
                    item.Key,
                    out var completion))
            {
                completion.TrySetException(
                    exception);
            }
        }
    }

    private static bool HasExited(
        Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch
        {
            return true;
        }
    }

    private static void TryKill(
        Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(
                    entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException
            .ThrowIf(
                _disposed,
                this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var process = _process;

        if (process is not null)
        {
            TryKill(process);

            try
            {
                process.Dispose();
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }

        FailPending(
            new ObjectDisposedException(
                nameof(BridgeClient)));

        _startLock.Dispose();
        _writeLock.Dispose();
    }
}