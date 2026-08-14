namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

public sealed class IdleShutdownBridgeClient : IBridgeClient
{
    private readonly IBridgeClient _inner;

    private readonly SemaphoreSlim _lifecycleLock =
        new(1, 1);

    private readonly object _stateLock =
        new();

    private CancellationTokenSource?
        _idleCancellation;

    private TimeSpan _idleTimeout;

    private long _activityGeneration;

    private bool _operationActive;
    private bool _disposed;

    public IdleShutdownBridgeClient(
        IBridgeClient inner,
        TimeSpan idleTimeout)
    {
        _inner =
            inner
            ?? throw new ArgumentNullException(
                nameof(inner));

        ValidateTimeout(idleTimeout);

        _idleTimeout = idleTimeout;
    }

    public TimeSpan IdleTimeout
    {
        get
        {
            lock (_stateLock)
            {
                return _idleTimeout;
            }
        }
    }

    public void SetIdleTimeout(
        TimeSpan idleTimeout)
    {
        ThrowIfDisposed();
        ValidateTimeout(idleTimeout);

        lock (_stateLock)
        {
            _idleTimeout = idleTimeout;

            _activityGeneration++;

            CancelIdleShutdownNoLock();

            if (!_operationActive)
            {
                ScheduleIdleShutdownNoLock();
            }
        }
    }

    public Task<BridgePingResult> PingAsync(
        CancellationToken cancellationToken = default)
    {
        return ExecuteTrackedAsync(
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
        return ExecuteTrackedAsync(
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
        return ExecuteTrackedAsync(
            token =>
                _inner.GetByIdAsync(
                    gameId,
                    token),
            cancellationToken);
    }

    public async Task ShutdownAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _lifecycleLock.WaitAsync(
            cancellationToken);

        try
        {
            lock (_stateLock)
            {
                _activityGeneration++;

                CancelIdleShutdownNoLock();

                _operationActive = true;
            }

            await _inner.ShutdownAsync(
                cancellationToken);
        }
        finally
        {
            lock (_stateLock)
            {
                _operationActive = false;
            }

            _lifecycleLock.Release();
        }
    }

    private async Task<T> ExecuteTrackedAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        await _lifecycleLock.WaitAsync(
            cancellationToken);

        try
        {
            lock (_stateLock)
            {
                _operationActive = true;

                _activityGeneration++;

                CancelIdleShutdownNoLock();
            }

            return await operation(
                cancellationToken);
        }
        finally
        {
            lock (_stateLock)
            {
                _operationActive = false;

                ScheduleIdleShutdownNoLock();
            }

            _lifecycleLock.Release();
        }
    }

    private void ScheduleIdleShutdownNoLock()
    {
        if (
            _disposed
            || _operationActive
            || _idleTimeout <= TimeSpan.Zero)
        {
            return;
        }

        CancelIdleShutdownNoLock();

        var cancellation =
            new CancellationTokenSource();

        _idleCancellation =
            cancellation;

        var generation =
            _activityGeneration;

        var delay =
            _idleTimeout;

        _ =
            ShutdownAfterIdleAsync(
                cancellation,
                generation,
                delay);
    }

    private async Task ShutdownAfterIdleAsync(
        CancellationTokenSource cancellation,
        long generation,
        TimeSpan delay)
    {
        try
        {
            await Task.Delay(
                delay,
                cancellation.Token);

            await _lifecycleLock.WaitAsync(
                cancellation.Token);

            try
            {
                lock (_stateLock)
                {
                    if (
                        _disposed
                        || _operationActive
                        || generation !=
                            _activityGeneration
                        || !ReferenceEquals(
                            _idleCancellation,
                            cancellation))
                    {
                        return;
                    }

                    _idleCancellation = null;

                    _operationActive = true;
                }

                await _inner.ShutdownAsync(
                    CancellationToken.None);
            }
            finally
            {
                lock (_stateLock)
                {
                    _operationActive = false;
                }

                _lifecycleLock.Release();
            }
        }
        catch (OperationCanceledException)
            when (cancellation.IsCancellationRequested)
        {
            // New activity or a setting change
            // superseded this idle timeout.
        }
        catch (ObjectDisposedException)
        {
            // Plugin shutdown raced the idle task.
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    private void CancelIdleShutdownNoLock()
    {
        var cancellation =
            _idleCancellation;

        _idleCancellation = null;

        if (cancellation is null)
        {
            return;
        }

        try
        {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void ValidateTimeout(
        TimeSpan idleTimeout)
    {
        if (idleTimeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idleTimeout));
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    public void Dispose()
    {
        lock (_stateLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _activityGeneration++;

            CancelIdleShutdownNoLock();
        }

        _inner.Dispose();
    }
}