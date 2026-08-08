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
        var plugin = new PluginMain();

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
    public void QueryReturnsStaticResult()
    {
        var plugin = new PluginMain();

        var query = new Query(
            "hltb test",
            "hltb");

        var results = plugin.Query(query);

        Assert.HasCount(1, results);

        var result = results[0];

        Assert.AreEqual(
            "HowLongToBeat plugin is running",
            result.Title);

        Assert.AreEqual(
            "Received: test",
            result.SubTitle);

        Assert.AreEqual(
            "test",
            result.QueryTextDisplay);

        Assert.AreEqual(100, result.Score);

        Assert.IsTrue(
            result.DisableUsageBasedScoring);
    }

    [TestMethod]
    public void EmptySearchShowsInstruction()
    {
        var plugin = new PluginMain();

        var query = new Query(
            "hltb",
            "hltb");

        var results = plugin.Query(query);

        Assert.HasCount(1, results);

        Assert.AreEqual(
            "Type a game name after hltb.",
            results[0].SubTitle);
    }

    [TestMethod]
    public void QueryPreservesMultiWordSearch()
    {
        var plugin = new PluginMain();

        var query = new Query(
            "hltb Elden Ring",
            "hltb");

        var results = plugin.Query(query);

        Assert.AreEqual(
            "Received: Elden Ring",
            results[0].SubTitle);
    }
}