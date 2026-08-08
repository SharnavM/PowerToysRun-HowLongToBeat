using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using PluginMain =
    Community.PowerToys.Run.Plugin.HowLongToBeat.Main;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class PluginManifestTests
{
    private static JsonElement LoadManifest()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "plugin.json");

        Assert.IsTrue(
            File.Exists(path),
            $"plugin.json was not copied to test output: {path}");

        using var document = JsonDocument.Parse(
            File.ReadAllText(path));

        return document.RootElement.Clone();
    }

    [TestMethod]
    public void ManifestIdMatchesMainPluginId()
    {
        var manifest = LoadManifest();

        var id = manifest
            .GetProperty("ID")
            .GetString();

        Assert.AreEqual(
            PluginMain.PluginID,
            id);
    }

    [TestMethod]
    public void ManifestUsesExpectedActionKeyword()
    {
        var manifest = LoadManifest();

        Assert.AreEqual(
            "hltb",
            manifest
                .GetProperty("ActionKeyword")
                .GetString());
    }

    [TestMethod]
    public void ManifestUsesExpectedPluginName()
    {
        var manifest = LoadManifest();

        Assert.AreEqual(
            "HowLongToBeat",
            manifest
                .GetProperty("Name")
                .GetString());
    }

    [TestMethod]
    public void ManifestReferencesPluginDll()
    {
        var manifest = LoadManifest();

        Assert.AreEqual(
            "Community.PowerToys.Run.Plugin.HowLongToBeat.dll",
            manifest
                .GetProperty("ExecuteFileName")
                .GetString());
    }

    [TestMethod]
    public void PluginIsExclusiveNotGlobal()
    {
        var manifest = LoadManifest();

        Assert.IsFalse(
            manifest
                .GetProperty("IsGlobal")
                .GetBoolean());
    }

    [TestMethod]
    public void DynamicLoadingIsDisabled()
    {
        var manifest = LoadManifest();

        Assert.IsFalse(
            manifest
                .GetProperty("DynamicLoading")
                .GetBoolean());
    }
}