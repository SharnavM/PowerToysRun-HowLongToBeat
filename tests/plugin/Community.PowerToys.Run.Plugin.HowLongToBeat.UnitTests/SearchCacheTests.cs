using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Caching;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class SearchCacheTests
{
    [TestMethod]
    public void NormalizedEquivalentQueryHitsCache()
    {
        var cache =
            new SearchCache();

        var result =
            Response();

        cache.Set(
            "Resident Evil 4",
            BridgeSearchMode.All,
            result);

        var found =
            cache.TryGet(
                "resident   evil 4",
                BridgeSearchMode.All,
                out var cached);

        Assert.IsTrue(found);
        Assert.AreSame(result, cached);
    }

    [TestMethod]
    public void DifferentSearchModeDoesNotHit()
    {
        var cache =
            new SearchCache();

        cache.Set(
            "Elden Ring",
            BridgeSearchMode.All,
            Response());

        var found =
            cache.TryGet(
                "Elden Ring",
                BridgeSearchMode.DlcOnly,
                out _);

        Assert.IsFalse(found);
    }

    [TestMethod]
    public void SuccessfulEntryExpires()
    {
        var now =
            new DateTimeOffset(
                2026,
                8,
                14,
                0,
                0,
                0,
                TimeSpan.Zero);

        var cache =
            new SearchCache(
                successTtl:
                    TimeSpan.FromMinutes(15),
                clock:
                    () => now);

        cache.Set(
            "Elden Ring",
            BridgeSearchMode.All,
            Response());

        now =
            now.AddMinutes(16);

        Assert.IsFalse(
            cache.TryGet(
                "Elden Ring",
                BridgeSearchMode.All,
                out _));
    }

    [TestMethod]
    public void EmptyEntryUsesShorterTtl()
    {
        var now =
            new DateTimeOffset(
                2026,
                8,
                14,
                0,
                0,
                0,
                TimeSpan.Zero);

        var cache =
            new SearchCache(
                successTtl:
                    TimeSpan.FromMinutes(15),
                emptyTtl:
                    TimeSpan.FromMinutes(2),
                clock:
                    () => now);

        cache.Set(
            "nothing",
            BridgeSearchMode.All,
            new BridgeSearchResult(
                [],
                0));

        now =
            now.AddMinutes(3);

        Assert.IsFalse(
            cache.TryGet(
                "nothing",
                BridgeSearchMode.All,
                out _));
    }

    private static BridgeSearchResult Response()
    {
        return new BridgeSearchResult(
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
                    null),
            ],
            1);
    }
}