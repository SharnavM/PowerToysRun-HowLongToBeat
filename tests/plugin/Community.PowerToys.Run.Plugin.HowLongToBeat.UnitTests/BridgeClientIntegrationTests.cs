using System.IO;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class BridgeClientIntegrationTests
{
    [TestMethod]
    public async Task PackagedBridgeRespondsToPing()
    {
        var executablePath = Environment.GetEnvironmentVariable("HLTB_BRIDGE_EXE");

        Assert.IsFalse(string.IsNullOrWhiteSpace(executablePath), "HLTB_BRIDGE_EXE is not set.");

        Assert.IsTrue(File.Exists(executablePath), $"Packaged bridge not found: {executablePath}");

        using var client = new BridgeClient(executablePath, TimeSpan.FromSeconds(5));

        var ping = await client.PingAsync();

        Assert.AreEqual(1, ping.ProtocolVersion);

        Assert.AreEqual(TestProjectInfo.ProjectVersion, ping.BridgeVersion);

        await client.ShutdownAsync();
    }

    [TestMethod]
    public void CreateDefaultUsesProvidedPluginDirectory()
    {
        var pluginDirectory = Path.Combine(Path.GetTempPath(), "hltb-plugin-test");

        using var client = BridgeClient.CreateDefault(pluginDirectory);

        // Construction must succeed without requiring
        // AppContext.BaseDirectory to contain the bridge.
        Assert.IsNotNull(client);
    }

    [TestMethod]
    public async Task LiveCyberpunkSearchReturnsResults()
    {
        if (
            !string.Equals(
                Environment.GetEnvironmentVariable("HLTB_RUN_LIVE_TESTS"),
                "1",
                StringComparison.Ordinal
            )
        )
        {
            Assert.Inconclusive("Set HLTB_RUN_LIVE_TESTS=1 to run live HLTB tests.");

            return;
        }

        var bridgePath = Environment.GetEnvironmentVariable("HLTB_BRIDGE_EXE");

        Assert.IsFalse(string.IsNullOrWhiteSpace(bridgePath));

        Assert.IsTrue(File.Exists(bridgePath));

        using var client = new BridgeClient(bridgePath!, TimeSpan.FromSeconds(10));

        var result = await client.SearchAsync("Cyberpunk");

        Assert.IsGreaterThan(0, result.Count);

        Assert.IsTrue(
            result.Results.Any(game => game.GameId == 2127 && game.Name == "Cyberpunk 2077")
        );

        await client.ShutdownAsync();
    }
}
