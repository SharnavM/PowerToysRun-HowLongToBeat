using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class GameResultFormatterTests
{
    [TestMethod]
    public void FormatsHoursAndMinutes()
    {
        Assert.AreEqual("60h 5m", GameResultFormatter.FormatDuration(216306));
    }

    [TestMethod]
    public void FormatsExactHours()
    {
        Assert.AreEqual("136h", GameResultFormatter.FormatDuration(489600));
    }

    [TestMethod]
    public void FormatsSubHourDuration()
    {
        Assert.AreEqual("57m", GameResultFormatter.FormatDuration(3420));
    }

    [TestMethod]
    public void MissingDurationUsesDash()
    {
        Assert.AreEqual("-", GameResultFormatter.FormatDuration(null));
    }

    [TestMethod]
    public void DlcTitleIncludesTypeAndYear()
    {
        var game = new BridgeGame(
            1,
            "Expansion",
            null,
            "dlc",
            2024,
            [],
            null,
            null,
            null,
            null,
            1.0,
            null,
            null
        );

        Assert.AreEqual("Expansion [DLC] (2024)", GameResultFormatter.BuildTitle(game));
    }

    [TestMethod]
    public void BuildsClipboardSummary()
    {
        var game = new BridgeGame(
            68151,
            "Elden Ring",
            null,
            "game",
            2022,
            ["PC"],
            216306,
            364346,
            489602,
            null,
            1.0,
            null,
            null
        );

        var text = GameResultFormatter.BuildClipboardText(game);

        StringAssert.Contains(text, "Elden Ring (2022)");

        StringAssert.Contains(text, "Main: 60h 5m");

        StringAssert.Contains(text, "Completionist:");
    }
}
