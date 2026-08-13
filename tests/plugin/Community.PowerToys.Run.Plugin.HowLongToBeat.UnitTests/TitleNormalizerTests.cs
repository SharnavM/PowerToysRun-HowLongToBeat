using Community.PowerToys.Run.Plugin.HowLongToBeat.Ranking;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests;

[TestClass]
public sealed class TitleNormalizerTests
{
    [TestMethod]
    public void RemovesPunctuation()
    {
        Assert.AreEqual(
            "resident evil 4 separate ways",
            TitleNormalizer.Normalize(
                "Resident Evil 4: Separate Ways"));
    }

    [TestMethod]
    public void PreservesNumbers()
    {
        Assert.AreNotEqual(
            TitleNormalizer.Normalize(
                "Modern Warfare 2"),
            TitleNormalizer.Normalize(
                "Modern Warfare 3"));
    }

    [TestMethod]
    public void RemovesDiacritics()
    {
        Assert.AreEqual(
            "pokemon",
            TitleNormalizer.Normalize(
                "Pokémon"));
    }

    [TestMethod]
    public void CollapsesWhitespace()
    {
        Assert.AreEqual(
            "elden ring",
            TitleNormalizer.Normalize(
                " Elden   Ring "));
    }
}