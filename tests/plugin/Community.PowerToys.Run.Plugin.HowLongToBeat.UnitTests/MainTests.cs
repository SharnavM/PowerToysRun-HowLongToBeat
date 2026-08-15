using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Wox.Plugin;

using PluginMain =
    Community.PowerToys.Run.Plugin.HowLongToBeat.Main;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class MainTests
{
    [TestMethod]
    public void PluginMetadataIsCorrect()
    {
        using var plugin =
            new PluginMain(
                new FakeBridgeClient());

        Assert.AreEqual(
            "HowLongToBeat",
            plugin.Name);

        Assert.AreEqual(
            "Search game completion times on HowLongToBeat.",
            plugin.Description);

        Assert.AreEqual(
            "0ADE2E6A74FF4E1AADBAABE8F06FFCD0",
            PluginMain.PluginID);
    }

    [TestMethod]
    public void EmptySearchShowsHelp()
    {
        var bridge =
            new FakeBridgeClient();

        using var plugin =
            new PluginMain(bridge);

        var results =
            plugin.Query(
                new Query(
                    "hltb",
                    "hltb"));

        Assert.HasCount(
            1,
            results);

        Assert.AreEqual(
            "Search HowLongToBeat",
            results[0].Title);

        Assert.AreEqual(0, bridge.SearchCallCount);
    }

    [TestMethod]
    public void ImmediateQueryDoesNotUseBridge()
    {
        var bridge =
            new FakeBridgeClient();

        using var plugin =
            new PluginMain(bridge);

        var results =
            plugin.Query(
                new Query(
                    "hltb Elden Ring",
                    "hltb"));

        Assert.HasCount(
            1,
            results);

        Assert.AreEqual(
            "Searching HowLongToBeat…",
            results[0].Title);

        Assert.AreEqual(
            "Looking up \"Elden Ring\"",
            results[0].SubTitle);

        Assert.AreEqual(0, bridge.SearchCallCount);
    }

    [TestMethod]
    public void DelayedQueryReturnsGameResults()
    {
        var bridge =
            new FakeBridgeClient
            {
                SearchHandler =
                    (_, _, _) =>
                        Task.FromResult(
                            SearchResponse(
                                EldenRing())),
            };

        using var plugin =
            new PluginMain(bridge);

        var query =
            new Query(
                "hltb Elden Ring",
                "hltb");

        plugin.Query(query);

        var results =
            plugin.Query(
                query,
                delayedExecution: true);

        Assert.AreEqual(
            1,
            bridge.SearchCallCount);

        Assert.HasCount(
            1,
            results);

        Assert.AreEqual(
            "Elden Ring (2022)",
            results[0].Title);

        Assert.AreEqual(
            "Main 60h 5m • " +
            "Main + Extras 101h 12m • " +
            "Completionist 136h",
            results[0].SubTitle);
    }

    [TestMethod]
    public void DelayedQueryHandlesNoResults()
    {
        var bridge =
            new FakeBridgeClient
            {
                SearchHandler =
                    (_, _, _) =>
                        Task.FromResult(
                            SearchResponse()),
            };

        using var plugin =
            new PluginMain(bridge);

        var query =
            new Query(
                "hltb xyzabc",
                "hltb");

        plugin.Query(query);

        var results =
            plugin.Query(
                query,
                delayedExecution: true);

        Assert.AreEqual(
            "No HowLongToBeat results",
            results[0].Title);
    }

    [TestMethod]
    public void BridgeFailureProducesUsefulResult()
    {
        var bridge =
            new FakeBridgeClient
            {
                SearchHandler =
                    (_, _, _) =>
                        Task.FromException<BridgeSearchResult>(
                            new BridgeException(
                                "upstream_error",
                                "HLTB failed.")),
            };

        using var plugin =
            new PluginMain(bridge);

        var query =
            new Query(
                "hltb Elden Ring",
                "hltb");

        plugin.Query(query);

        var results =
            plugin.Query(
                query,
                delayedExecution: true);

        Assert.AreEqual(
            "HowLongToBeat unavailable - search in browser",
            results[0].Title);
        Assert.AreEqual(
            "Press Enter to continue on howlongtobeat.com.",
            results[0].SubTitle);
    }

    [TestMethod]
    public async Task NewQueryCancelsPreviousSearch()
    {
        var firstStarted =
            new TaskCompletionSource(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

        var bridge =
            new FakeBridgeClient();

        bridge.SearchHandler =
            async (
                query,
                _,
                cancellationToken) =>
            {
                if (query == "Elden Ring")
                {
                    firstStarted.TrySetResult();

                    await Task.Delay(
                        Timeout.Infinite,
                        cancellationToken);
                }

                return SearchResponse(
                    Sekiro());
            };

        using var plugin =
            new PluginMain(bridge);

        var firstQuery =
            new Query(
                "hltb Elden Ring",
                "hltb");

        plugin.Query(firstQuery);

        var firstTask =
            Task.Run(
                () => plugin.Query(
                    firstQuery,
                    delayedExecution: true));

        await firstStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2));

        var secondQuery =
            new Query(
                "hltb Sekiro",
                "hltb");

        plugin.Query(secondQuery);

        var secondTask =
            Task.Run(
                () => plugin.Query(
                    secondQuery,
                    delayedExecution: true));

        var firstResults =
            await firstTask;

        var secondResults =
            await secondTask;

        Assert.IsEmpty(firstResults);

        Assert.HasCount(
            1,
            secondResults);

        Assert.AreEqual(
            "Sekiro: Shadows Die Twice (2019)",
            secondResults[0].Title);
    }

    
    [TestMethod]
    public void DlcModifierUsesDlcOnlyBridgeMode()
    {
        var bridge =
            new FakeBridgeClient
            {
                SearchHandler =
                    (_, _, _) =>
                        Task.FromResult(
                            SearchResponse(
                                EldenRing())),
            };

        using var plugin =
            new PluginMain(bridge);

        var query =
            new Query(
                "hltb Elden Ring --dlc",
                "hltb");

        plugin.Query(query);

        plugin.Query(
            query,
            delayedExecution: true);

        Assert.AreEqual(
            BridgeSearchMode.DlcOnly,
            bridge.LastSearchMode);
    }

    [TestMethod]
    public void InvalidModifierDoesNotSearchBridge()
    {
        var bridge =
            new FakeBridgeClient();

        using var plugin =
            new PluginMain(bridge);

        var query =
            new Query(
                "hltb Elden Ring --year banana",
                "hltb");

        var immediate =
            plugin.Query(query);

        Assert.AreEqual(
            "Invalid year",
            immediate[0].Title);

        Assert.AreEqual(
            0,
            bridge.SearchCallCount);
    }

    [TestMethod]
    public void DirectIdUsesGetByIdInsteadOfSearch()
    {
        var bridge =
            new FakeBridgeClient
            {
                GetByIdHandler =
                    (_, _) =>
                        Task.FromResult<BridgeGame?>(
                            EldenRing()),
            };

        using var plugin =
            new PluginMain(bridge);

        var query =
            new Query(
                "hltb id:68151",
                "hltb");

        plugin.Query(query);

        var results =
            plugin.Query(
                query,
                delayedExecution: true);

        Assert.AreEqual(
            0,
            bridge.SearchCallCount);

        Assert.AreEqual(
            1,
            bridge.GetByIdCallCount);

        Assert.AreEqual(
            "Elden Ring (2022)",
            results[0].Title);
    }

    [TestMethod]
    public void RankingModifiersReuseCachedSearchResponse()
    {
        var bridge =
            new FakeBridgeClient
            {
                SearchHandler =
                    (_, _, _) =>
                        Task.FromResult(
                            SearchResponse(
                                EldenRing())),
            };

        using var plugin =
            new PluginMain(bridge);

        var first =
            new Query(
                "hltb Elden Ring",
                "hltb");

        plugin.Query(first);

        plugin.Query(
            first,
            delayedExecution: true);

        var second =
            new Query(
                "hltb Elden Ring --year 2022",
                "hltb");

        plugin.Query(second);

        plugin.Query(
            second,
            delayedExecution: true);

        Assert.AreEqual(
            1,
            bridge.SearchCallCount);
    }

    [TestMethod]
    public async Task SearchDelayDefersBridgeRequest()
    {
        var bridge =
            new FakeBridgeClient
            {
                SearchHandler =
                    (_, _, _) =>
                        Task.FromResult(
                            SearchResponse(
                                EldenRing())),
            };

        using var plugin =
            new PluginMain(
                bridge,
                searchDelayMilliseconds: 250);

        var query =
            new Query(
                "hltb Elden Ring",
                "hltb");

        plugin.Query(query);

        var delayedTask =
            Task.Run(
                () => plugin.Query(
                    query,
                    delayedExecution: true));

        await Task.Delay(60);

        Assert.AreEqual(
            0,
            bridge.SearchCallCount);

        var results =
            await delayedTask;

        Assert.AreEqual(
            1,
            bridge.SearchCallCount);

        Assert.HasCount(
            1,
            results);
    }

    [TestMethod]
    public void SearchDelayDefaultsToFourHundredMilliseconds()
    {
        using var plugin =
            new PluginMain(
                new FakeBridgeClient());

        var option =
            plugin.AdditionalOptions.Single(
                item =>
                    item.Key ==
                    "SearchDelayMilliseconds");

        Assert.AreEqual(
            400d,
            option.NumberValue);

        Assert.AreEqual(
            0d,
            option.NumberBoxMin);

        Assert.AreEqual(
            2000d,
            option.NumberBoxMax);
    }

    private static BridgeSearchResult SearchResponse(
        params BridgeGame[] games)
    {
        return new BridgeSearchResult(
            games,
            games.Length);
    }

    private static BridgeGame EldenRing()
    {
        return new BridgeGame(
            68151,
            "Elden Ring",
            "Elden Ring Tarnished Edition",
            "game",
            2022,
            ["PC", "PlayStation 5"],
            216306,
            364346,
            489602,
            379810,
            1.0,
            "https://howlongtobeat.com/game/68151",
            null);
    }

    private static BridgeGame Sekiro()
    {
        return new BridgeGame(
            57415,
            "Sekiro: Shadows Die Twice",
            null,
            "game",
            2019,
            ["PC"],
            108000,
            null,
            null,
            null,
            1.0,
            "https://howlongtobeat.com/game/57415",
            null);
    }

    private sealed class FakeBridgeClient :
        IBridgeClient
    {
        public int SearchCallCount { get; private set; }

        public Func<
            string,
            BridgeSearchMode,
            CancellationToken,
            Task<BridgeSearchResult>>?
            SearchHandler { get; set; }

        public BridgeSearchMode? LastSearchMode
        {
            get;
            private set;
        }

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public Func<
            int,
            CancellationToken,
            Task<BridgeGame?>>?
            GetByIdHandler
        {
            get;
            set;
        }

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
            SearchCallCount++;

            LastSearchMode = mode;

            if (SearchHandler is null)
            {
                return Task.FromResult(
                    SearchResponse());
            }

            return SearchHandler(
                query,
                mode,
                cancellationToken);
        }

        public Task<BridgeGame?> GetByIdAsync(
            int gameId,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            if (GetByIdHandler is null)
            {
                return Task.FromResult<BridgeGame?>(
                    null);
            }

            return GetByIdHandler(
                gameId,
                cancellationToken);
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