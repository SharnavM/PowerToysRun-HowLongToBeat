using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class IdleShutdownBridgeClientTests
{
    [TestMethod]
    public async Task ShutsDownAfterIdleTimeout()
    {
        var inner =
            new FakeBridgeClient();

        using var client =
            new IdleShutdownBridgeClient(
                inner,
                TimeSpan.FromMilliseconds(50));

        await client.PingAsync();

        await inner.ShutdownObserved.Task.WaitAsync(
            TimeSpan.FromSeconds(2));

        Assert.AreEqual(
            1,
            inner.ShutdownCallCount);
    }

    [TestMethod]
    public async Task NewActivityResetsIdleTimeout()
    {
        var inner =
            new FakeBridgeClient();

        using var client =
            new IdleShutdownBridgeClient(
                inner,
                TimeSpan.FromMilliseconds(250));

        await client.PingAsync();

        await Task.Delay(100);

        await client.PingAsync();

        await Task.Delay(175);

        Assert.AreEqual(
            0,
            inner.ShutdownCallCount);

        await inner.ShutdownObserved.Task.WaitAsync(
            TimeSpan.FromSeconds(2));

        Assert.AreEqual(
            1,
            inner.ShutdownCallCount);
    }

    [TestMethod]
    public async Task ZeroTimeoutDisablesIdleShutdown()
    {
        var inner =
            new FakeBridgeClient();

        using var client =
            new IdleShutdownBridgeClient(
                inner,
                TimeSpan.Zero);

        await client.PingAsync();

        await Task.Delay(150);

        Assert.AreEqual(
            0,
            inner.ShutdownCallCount);
    }

    [TestMethod]
    public async Task ChangingTimeoutReschedulesShutdown()
    {
        var inner =
            new FakeBridgeClient();

        using var client =
            new IdleShutdownBridgeClient(
                inner,
                TimeSpan.FromMinutes(10));

        await client.PingAsync();

        client.SetIdleTimeout(
            TimeSpan.FromMilliseconds(50));

        await inner.ShutdownObserved.Task.WaitAsync(
            TimeSpan.FromSeconds(2));

        Assert.AreEqual(
            1,
            inner.ShutdownCallCount);
    }

    private sealed class FakeBridgeClient :
        IBridgeClient
    {
        public int ShutdownCallCount
        {
            get;
            private set;
        }

        public TaskCompletionSource
            ShutdownObserved
        {
            get;
        } =
            new(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

        public Task<BridgePingResult> PingAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new BridgePingResult(
                    1,
                    TestProjectInfo.ProjectVersion));
        }

        public Task<BridgeSearchResult> SearchAsync(
            string query,
            BridgeSearchMode mode =
                BridgeSearchMode.All,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new BridgeSearchResult(
                    [],
                    0));
        }

        public Task<BridgeGame?> GetByIdAsync(
            int gameId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<BridgeGame?>(
                null);
        }

        public Task ShutdownAsync(
            CancellationToken cancellationToken = default)
        {
            ShutdownCallCount++;

            ShutdownObserved.TrySetResult();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}