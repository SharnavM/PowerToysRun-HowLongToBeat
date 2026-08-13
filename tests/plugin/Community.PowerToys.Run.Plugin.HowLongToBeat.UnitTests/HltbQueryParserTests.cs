using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Parsing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class HltbQueryParserTests
{
    [TestMethod]
    public void ParsesPlainTitle()
    {
        var result =
            HltbQueryParser.Parse(
                "Elden Ring");

        Assert.IsTrue(result.IsValid);

        Assert.AreEqual(
            "Elden Ring",
            result.SearchText);

        Assert.AreEqual(
            BridgeSearchMode.All,
            result.SearchMode);
    }

    [TestMethod]
    public void ParsesQuotedTitle()
    {
        var result =
            HltbQueryParser.Parse(
                "\"Resident Evil 4\" --year 2005");

        Assert.AreEqual(
            "Resident Evil 4",
            result.SearchText);

        Assert.AreEqual(
            2005,
            result.Year);
    }

    [TestMethod]
    public void ParsesYearEqualsSyntax()
    {
        var result =
            HltbQueryParser.Parse(
                "Resident Evil 4 --year=2023");

        Assert.AreEqual(
            2023,
            result.Year);
    }

    [TestMethod]
    public void ParsesPlatformAlias()
    {
        var result =
            HltbQueryParser.Parse(
                "Resident Evil 4 --platform gc");

        Assert.AreEqual(
            "Nintendo GameCube",
            result.Platform);
    }

    [TestMethod]
    public void ParsesQuotedPlatform()
    {
        var result =
            HltbQueryParser.Parse(
                "Game --platform \"PlayStation 5\"");

        Assert.AreEqual(
            "PlayStation 5",
            result.Platform);
    }

    [TestMethod]
    public void ParsesDlcOnly()
    {
        var result =
            HltbQueryParser.Parse(
                "Elden Ring --dlc");

        Assert.AreEqual(
            BridgeSearchMode.DlcOnly,
            result.SearchMode);
    }

    [TestMethod]
    public void ParsesHideDlc()
    {
        var result =
            HltbQueryParser.Parse(
                "Elden Ring --no-dlc");

        Assert.AreEqual(
            BridgeSearchMode.HideDlc,
            result.SearchMode);
    }

    [TestMethod]
    public void RejectsConflictingDlcOptions()
    {
        var result =
            HltbQueryParser.Parse(
                "Elden Ring --dlc --no-dlc");

        Assert.IsFalse(
            result.IsValid);

        Assert.AreEqual(
            "Conflicting options",
            result.ErrorTitle);
    }

    [TestMethod]
    public void RejectsInvalidYear()
    {
        var result =
            HltbQueryParser.Parse(
                "Elden Ring --year banana");

        Assert.IsFalse(
            result.IsValid);

        Assert.AreEqual(
            "Invalid year",
            result.ErrorTitle);
    }

    [TestMethod]
    public void RejectsMissingPlatform()
    {
        var result =
            HltbQueryParser.Parse(
                "Elden Ring --platform");

        Assert.IsFalse(
            result.IsValid);

        Assert.AreEqual(
            "Missing platform",
            result.ErrorTitle);
    }

    [TestMethod]
    public void RejectsUnknownOption()
    {
        var result =
            HltbQueryParser.Parse(
                "Elden Ring --banana");

        Assert.IsFalse(
            result.IsValid);

        Assert.AreEqual(
            "Unknown option",
            result.ErrorTitle);
    }

    [TestMethod]
    public void ParsesGameId()
    {
        var result =
            HltbQueryParser.Parse(
                "id:68151");

        Assert.IsTrue(
            result.IsIdLookup);

        Assert.AreEqual(
            68151,
            result.GameId);

        Assert.IsNull(
            result.SearchText);
    }

    [TestMethod]
    public void RejectsInvalidGameId()
    {
        var result =
            HltbQueryParser.Parse(
                "id:abc");

        Assert.IsFalse(
            result.IsValid);

        Assert.AreEqual(
            "Invalid HowLongToBeat ID",
            result.ErrorTitle);
    }

    [TestMethod]
    public void RejectsModifiersWithGameId()
    {
        var result =
            HltbQueryParser.Parse(
                "id:68151 --year 2022");

        Assert.IsFalse(
            result.IsValid);

        Assert.AreEqual(
            "Invalid ID lookup",
            result.ErrorTitle);
    }

    [TestMethod]
    public void RejectsUnclosedQuote()
    {
        var result =
            HltbQueryParser.Parse(
                "\"Resident Evil 4");

        Assert.IsFalse(
            result.IsValid);

        Assert.AreEqual(
            "Invalid query",
            result.ErrorTitle);
    }
}