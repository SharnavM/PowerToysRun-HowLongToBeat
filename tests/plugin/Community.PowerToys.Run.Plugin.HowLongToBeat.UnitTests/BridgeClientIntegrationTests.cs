using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class BridgeClientIntegrationTests
{
    [TestMethod]
    public async Task PackagedBridgeRespondsToPing()
    {
        var executablePath =
            Environment.GetEnvironmentVariable(
                "HLTB_BRIDGE_EXE");

        Assert.IsFalse(
            string.IsNullOrWhiteSpace(
                executablePath),
            "HLTB_BRIDGE_EXE is not set.");

        Assert.IsTrue(
            File.Exists(executablePath),
            $"Packaged bridge not found: {executablePath}");

        using var client =
            new BridgeClient(
                executablePath,
                TimeSpan.FromSeconds(5));

        var ping =
            await client.PingAsync();

        Assert.AreEqual(
            1,
            ping.ProtocolVersion);

        Assert.AreEqual(
            "0.1.0",
            ping.BridgeVersion);

        await client.ShutdownAsync();
    }
}