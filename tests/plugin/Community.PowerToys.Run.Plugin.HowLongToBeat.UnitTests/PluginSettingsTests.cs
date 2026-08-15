using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PluginMain = Community.PowerToys.Run.Plugin.HowLongToBeat.Main;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class PluginSettingsTests
{
    [TestMethod]
    public void IdleTimeoutDefaultsToTenMinutes()
    {
        using var plugin = new PluginMain(new FakeBridgeClient());

        var option = plugin.AdditionalOptions.Single(item =>
            item.Key == "BridgeIdleTimeoutMinutes"
        );

        Assert.AreEqual("BridgeIdleTimeoutMinutes", option.Key);

        Assert.AreEqual(
            PluginAdditionalOption.AdditionalOptionType.Numberbox,
            option.PluginOptionType
        );

        Assert.AreEqual(10d, option.NumberValue);

        Assert.AreEqual(0d, option.NumberBoxMin);

        Assert.AreEqual(120d, option.NumberBoxMax);
    }

    [TestMethod]
    public void UpdateSettingsChangesIdleTimeout()
    {
        var inner = new FakeBridgeClient();

        using var idle = new IdleShutdownBridgeClient(inner, TimeSpan.FromMinutes(10));

        using var plugin = new PluginMain(idle);

        plugin.UpdateSettings(
            new PowerLauncherPluginSettings
            {
                AdditionalOptions =
                [
                    new PluginAdditionalOption
                    {
                        Key = "BridgeIdleTimeoutMinutes",

                        NumberValue = 3,
                    },
                ],
            }
        );

        Assert.AreEqual(TimeSpan.FromMinutes(3), idle.IdleTimeout);
    }

    [TestMethod]
    public void ZeroSettingDisablesTimeout()
    {
        var inner = new FakeBridgeClient();

        using var idle = new IdleShutdownBridgeClient(inner, TimeSpan.FromMinutes(10));

        using var plugin = new PluginMain(idle);

        plugin.UpdateSettings(
            new PowerLauncherPluginSettings
            {
                AdditionalOptions =
                [
                    new PluginAdditionalOption
                    {
                        Key = "BridgeIdleTimeoutMinutes",

                        NumberValue = 0,
                    },
                ],
            }
        );

        Assert.AreEqual(TimeSpan.Zero, idle.IdleTimeout);
    }

    private sealed class FakeBridgeClient : IBridgeClient
    {
        public Task<BridgePingResult> PingAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new BridgePingResult(1, TestProjectInfo.ProjectVersion));

        public Task<BridgeSearchResult> SearchAsync(
            string query,
            BridgeSearchMode mode = BridgeSearchMode.All,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new BridgeSearchResult([], 0));

        public Task<BridgeGame?> GetByIdAsync(
            int gameId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<BridgeGame?>(null);

        public Task ShutdownAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Dispose() { }
    }
}
