using System.Text.Json;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class BridgeModelsTests
{
    [TestMethod]
    public void SearchResponsePreservesRawSeconds()
    {
        const string json =
            """
            {
              "results": [
                {
                  "gameId": 68151,
                  "name": "Elden Ring",
                  "alias": "Elden Ring Tarnished Edition",
                  "type": "game",
                  "releaseYear": 2022,
                  "platforms": [
                    "PC",
                    "PlayStation 5"
                  ],
                  "mainSeconds": 216306,
                  "mainExtraSeconds": 364346,
                  "completionistSeconds": 489602,
                  "allStylesSeconds": 379810,
                  "similarity": 1.0,
                  "url": "https://howlongtobeat.com/game/68151",
                  "imageUrl": "https://howlongtobeat.com/games/68151_Elden_Ring.jpg"
                }
              ],
              "count": 1
            }
            """;

        var result =
            JsonSerializer.Deserialize<BridgeSearchResult>(
                json);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.HasCount(1, result.Results);

        var game = result.Results[0];

        Assert.AreEqual(
            68151,
            game.GameId);

        Assert.AreEqual(
            216306,
            game.MainSeconds);

        Assert.AreEqual(
            489602,
            game.CompletionistSeconds);
    }

    [TestMethod]
    public void MissingTimingDataRemainsNull()
    {
        const string json =
            """
            {
              "results": [
                {
                  "gameId": 132630,
                  "name": "Elden Ring: The Convergence",
                  "alias": null,
                  "type": "mod",
                  "releaseYear": null,
                  "platforms": ["PC"],
                  "mainSeconds": 72000,
                  "mainExtraSeconds": 102606,
                  "completionistSeconds": null,
                  "allStylesSeconds": 90454,
                  "similarity": 0.54,
                  "url": null,
                  "imageUrl": null
                }
              ],
              "count": 1
            }
            """;

        var result =
            JsonSerializer.Deserialize<BridgeSearchResult>(
                json);

        Assert.IsNotNull(result);

        var game = result.Results[0];

        Assert.IsNull(
            game.ReleaseYear);

        Assert.IsNull(
            game.CompletionistSeconds);
    }
}