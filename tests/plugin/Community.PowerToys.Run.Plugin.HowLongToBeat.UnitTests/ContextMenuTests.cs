using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Wox.Plugin;

using PluginMain =
    Community.PowerToys.Run.Plugin.HowLongToBeat.Main;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class ContextMenuTests
{
    [TestMethod]
    public void GameResultGetsThreeContextActions()
    {
        using var plugin =
            new PluginMain(
                new FakeBridgeClient());

        var result =
            new Result
            {
                ContextData =
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
                        "https://howlongtobeat.com/game/68151",
                        null),
            };

        var menus =
            plugin.LoadContextMenus(
                result);

        Assert.HasCount(
            3,
            menus);

        Assert.AreEqual(
            "Open on HowLongToBeat",
            menus[0].Title);

        Assert.AreEqual(
            "Copy completion times",
            menus[1].Title);

        Assert.AreEqual(
            "Copy HowLongToBeat link",
            menus[2].Title);
    }

    [TestMethod]
    public void NonGameResultHasNoContextActions()
    {
        using var plugin =
            new PluginMain(
                new FakeBridgeClient());

        var menus =
            plugin.LoadContextMenus(
                new Result());

        Assert.IsEmpty(
            menus);
    }

    private sealed class FakeBridgeClient :
        IBridgeClient
    {
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
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}