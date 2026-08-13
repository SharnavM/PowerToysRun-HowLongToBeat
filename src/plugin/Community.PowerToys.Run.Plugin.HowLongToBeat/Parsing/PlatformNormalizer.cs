using Community.PowerToys.Run.Plugin.HowLongToBeat.Ranking;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Parsing;

public static class PlatformNormalizer
{
    private static readonly Dictionary<
        string,
        string> Aliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pc"] = "PC",
            ["windows"] = "PC",

            ["ps"] = "PlayStation",
            ["playstation"] = "PlayStation",

            ["ps2"] = "PlayStation 2",
            ["playstation 2"] = "PlayStation 2",

            ["ps3"] = "PlayStation 3",
            ["playstation 3"] = "PlayStation 3",

            ["ps4"] = "PlayStation 4",
            ["playstation 4"] = "PlayStation 4",

            ["ps5"] = "PlayStation 5",
            ["playstation 5"] = "PlayStation 5",

            ["xb1"] = "Xbox One",
            ["xbox one"] = "Xbox One",

            ["x360"] = "Xbox 360",
            ["xbox 360"] = "Xbox 360",

            ["xsx"] = "Xbox Series X/S",
            ["xbox series"] = "Xbox Series X/S",
            ["xbox series x"] = "Xbox Series X/S",
            ["xbox series x s"] = "Xbox Series X/S",

            ["xbox"] = "Xbox",

            ["switch"] = "Nintendo Switch",
            ["nintendo switch"] = "Nintendo Switch",

            ["switch2"] = "Nintendo Switch 2",
            ["switch 2"] = "Nintendo Switch 2",
            ["nintendo switch 2"] = "Nintendo Switch 2",

            ["gc"] = "Nintendo GameCube",
            ["gamecube"] = "Nintendo GameCube",
            ["nintendo gamecube"] = "Nintendo GameCube",

            ["wii"] = "Wii",

            ["3ds"] = "Nintendo 3DS",
            ["nintendo 3ds"] = "Nintendo 3DS",
        };

    public static string Normalize(
        string platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            platform);

        var trimmed =
            platform.Trim();

        var key =
            TitleNormalizer.Normalize(
                trimmed);

        return Aliases.TryGetValue(
            key,
            out var canonical)
                ? canonical
                : trimmed;
    }

    public static bool Matches(
        string requestedPlatform,
        IEnumerable<string> gamePlatforms)
    {
        var requested =
            Normalize(requestedPlatform);

        var normalizedRequested =
            TitleNormalizer.Normalize(
                requested);

        foreach (var platform in gamePlatforms)
        {
            var candidate =
                TitleNormalizer.Normalize(
                    platform);

            if (candidate ==
                normalizedRequested)
            {
                return true;
            }

            if (
                normalizedRequested ==
                    "playstation"
                && candidate.StartsWith(
                    "playstation ",
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (
                normalizedRequested ==
                    "xbox"
                && candidate.StartsWith(
                    "xbox",
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}