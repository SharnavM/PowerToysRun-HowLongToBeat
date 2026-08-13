using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Parsing;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Ranking;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class GameRankerTests
{
    [TestMethod]
    public void ExactTitleBeatsRelatedHighSimilarityTitle()
    {
        var query =
            HltbQueryParser.Parse(
                "Elden Ring");

        var ranked =
            GameRanker.Rank(
                [
                    Game(
                        108888,
                        "Elden Ring GB",
                        2022,
                        0.87),

                    Game(
                        68151,
                        "Elden Ring",
                        2022,
                        1.0),
                ],
                query);

        Assert.AreEqual(
            68151,
            ranked[0].GameId);
    }

    [TestMethod]
    public void YearDisambiguatesResidentEvil4()
    {
        var query =
            HltbQueryParser.Parse(
                "Resident Evil 4 --year 2005");

        var ranked =
            GameRanker.Rank(
                [
                    ResidentEvil2023(),
                    ResidentEvil2005(),
                ],
                query);

        Assert.AreEqual(
            7720,
            ranked[0].GameId);
    }

    [TestMethod]
    public void PlatformDisambiguatesResidentEvil4()
    {
        var query =
            HltbQueryParser.Parse(
                "Resident Evil 4 --platform gc");

        var ranked =
            GameRanker.Rank(
                [
                    ResidentEvil2023(),
                    ResidentEvil2005(),
                ],
                query);

        Assert.AreEqual(
            7720,
            ranked[0].GameId);
    }

    [TestMethod]
    public void AliasCanPromoteRemake()
    {
        var query =
            HltbQueryParser.Parse(
                "Resident Evil 4 Remake");

        var ranked =
            GameRanker.Rank(
                [
                    ResidentEvil2005(),
                    ResidentEvil2023(),
                ],
                query);

        Assert.AreEqual(
            108881,
            ranked[0].GameId);
    }

    [TestMethod]
    public void ModReceivesSmallDefaultPenalty()
    {
        var query =
            HltbQueryParser.Parse(
                "Elden Ring");

        var baseGame =
            Game(
                68151,
                "Elden Ring",
                2022,
                1.0);

        var mod =
            Game(
                132630,
                "Elden Ring: The Convergence",
                null,
                0.54,
                type: "mod");

        var baseScore = 
            GameRanker.CalculateScore(
              baseGame,
              query
            );
        var modScore = 
            GameRanker.CalculateScore(
              mod,
              query
            );
        Assert.IsTrue(
          baseScore > modScore,
          $"Expected base game score {baseScore} " +
          $"to exceed mod score {modScore}.");
    }

    [TestMethod]
    public void OriginalOrderBreaksTrueTie()
    {
        var query =
            HltbQueryParser.Parse(
                "Resident Evil 4");

        var first =
            ResidentEvil2023();

        var second =
            ResidentEvil2005();

        var ranked =
            GameRanker.Rank(
                [first, second],
                query);

        Assert.AreEqual(
            first.GameId,
            ranked[0].GameId);
    }

    private static BridgeGame ResidentEvil2023()
    {
        return new BridgeGame(
            108881,
            "Resident Evil 4",
            "Resident Evil 4 Remake",
            "game",
            2023,
            [
                "PC",
                "PlayStation 4",
                "PlayStation 5",
                "Xbox Series X/S",
            ],
            58212,
            77904,
            232704,
            null,
            1.0,
            null,
            null);
    }

    private static BridgeGame ResidentEvil2005()
    {
        return new BridgeGame(
            7720,
            "Resident Evil 4",
            "Resident Evil 4 HD, Biohazard 4, Resident Evil 4: Wii Edition",
            "game",
            2005,
            [
                "Nintendo GameCube",
                "PC",
                "PlayStation 2",
                "Wii",
            ],
            56160,
            70452,
            115884,
            null,
            1.0,
            null,
            null);
    }

    private static BridgeGame Game(
        int id,
        string name,
        int? year,
        double similarity,
        string type = "game")
    {
        return new BridgeGame(
            id,
            name,
            null,
            type,
            year,
            ["PC"],
            3600,
            7200,
            10800,
            null,
            similarity,
            null,
            null);
    }
}