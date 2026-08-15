using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class ResilientBridgeClientTests
{
    [TestMethod]
    public async Task RetriesOnceAfterBridgeExit()
    {
        var inner = new FlakyBridgeClient(firstErrorCode: "bridge_exited");

        using var client = new ResilientBridgeClient(inner);

        var result = await client.SearchAsync("Elden Ring");

        Assert.AreEqual(2, inner.SearchCalls);

        Assert.AreEqual(1, inner.ShutdownCalls);

        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task DoesNotRetryUpstreamFailure()
    {
        var inner = new FlakyBridgeClient(firstErrorCode: "upstream_error");

        using var client = new ResilientBridgeClient(inner);

        await Assert.ThrowsExactlyAsync<BridgeException>(() => client.SearchAsync("Elden Ring"));

        Assert.AreEqual(1, inner.SearchCalls);

        Assert.AreEqual(0, inner.ShutdownCalls);
    }

    private sealed class FlakyBridgeClient : IBridgeClient
    {
        private readonly string _firstErrorCode;

        public FlakyBridgeClient(string firstErrorCode)
        {
            _firstErrorCode = firstErrorCode;
        }

        public int SearchCalls { get; private set; }

        public int ShutdownCalls { get; private set; }

        public Task<BridgeSearchResult> SearchAsync(
            string query,
            BridgeSearchMode mode = BridgeSearchMode.All,
            CancellationToken cancellationToken = default
        )
        {
            SearchCalls++;

            if (SearchCalls == 1)
            {
                return Task.FromException<BridgeSearchResult>(
                    new BridgeException(_firstErrorCode, "Test failure.")
                );
            }

            return Task.FromResult(
                new BridgeSearchResult(
                    [
                        new BridgeGame(
                            68151,
                            "Elden Ring",
                            null,
                            "game",
                            2022,
                            ["PC"],
                            216306,
                            364346,
                            489602,
                            379810,
                            1.0,
                            null,
                            null
                        ),
                    ],
                    1
                )
            );
        }

        public Task<BridgePingResult> PingAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new BridgePingResult(1, TestProjectInfo.ProjectVersion));

        public Task<BridgeGame?> GetByIdAsync(
            int gameId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<BridgeGame?>(null);

        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            ShutdownCalls++;
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
