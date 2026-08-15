namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

public sealed class ResilientBridgeClient :
    IBridgeClient
{
    private readonly IBridgeClient _inner;

    public ResilientBridgeClient(
        IBridgeClient inner)
    {
        _inner =
            inner
            ?? throw new ArgumentNullException(
                nameof(inner));
    }

    public Task<BridgePingResult> PingAsync(
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithRecoveryAsync(
            token =>
                _inner.PingAsync(token),
            cancellationToken);
    }

    public Task<BridgeSearchResult> SearchAsync(
        string query,
        BridgeSearchMode mode =
            BridgeSearchMode.All,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithRecoveryAsync(
            token =>
                _inner.SearchAsync(
                    query,
                    mode,
                    token),
            cancellationToken);
    }

    public Task<BridgeGame?> GetByIdAsync(
        int gameId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithRecoveryAsync(
            token =>
                _inner.GetByIdAsync(
                    gameId,
                    token),
            cancellationToken);
    }

    public Task ShutdownAsync(
        CancellationToken cancellationToken = default)
    {
        return _inner.ShutdownAsync(
            cancellationToken);
    }

    private async Task<T> ExecuteWithRecoveryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation(
                cancellationToken);
        }
        catch (BridgeException exception)
            when (
                IsRecoverable(exception)
                && !cancellationToken
                    .IsCancellationRequested)
        {
            await ResetBridgeAsync();

            // Exactly one retry.
            return await operation(
                cancellationToken);
        }
    }

    private async Task ResetBridgeAsync()
    {
        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(750));

        try
        {
            await _inner.ShutdownAsync(
                timeout.Token);
        }
        catch
        {
            // Reset is best-effort.
            // BridgeClient will still detect an exited
            // process and lazily create a replacement.
        }
    }

    private static bool IsRecoverable(
        BridgeException exception)
    {
        return exception.Code switch
        {
            "bridge_exited" => true,
            "bridge_read_error" => true,
            "write_error" => true,

            _ => false,
        };
    }

    public void Dispose()
    {
        _inner.Dispose();
    }
}