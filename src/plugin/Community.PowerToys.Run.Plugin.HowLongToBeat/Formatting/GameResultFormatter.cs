using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Formatting;

public static class GameResultFormatter
{
    public static string FormatDuration(int? seconds)
    {
        if (seconds is null or <= 0)
        {
            return "—";
        }

        var totalMinutes = (int)Math.Round(
            seconds.Value / 60.0,
            MidpointRounding.AwayFromZero);

        totalMinutes = Math.Max(1, totalMinutes);

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        if (hours == 0)
        {
            return $"{minutes}m";
        }

        if (minutes == 0)
        {
            return $"{hours}h";
        }

        return $"{hours}h {minutes}m";
    }

    public static string BuildTitle(BridgeGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var typeSuffix =
            game.Type?.ToLowerInvariant() switch
            {
                "dlc" => " [DLC]",
                "mod" => " [Mod]",
                _ => string.Empty,
            };

        var yearSuffix =
            game.ReleaseYear is int year
                ? $" ({year})"
                : string.Empty;

        return $"{game.Name}{typeSuffix}{yearSuffix}";
    }

    public static string BuildSubtitle(BridgeGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var parts = new List<string>();

        if (game.MainSeconds is > 0)
        {
            parts.Add(
                $"Main {FormatDuration(game.MainSeconds)}");
        }

        if (game.MainExtraSeconds is > 0)
        {
            parts.Add(
                $"Main + Extras " +
                $"{FormatDuration(game.MainExtraSeconds)}");
        }

        if (game.CompletionistSeconds is > 0)
        {
            parts.Add(
                $"Completionist " +
                $"{FormatDuration(game.CompletionistSeconds)}");
        }

        if (parts.Count == 0)
        {
            return "No completion-time estimates available";
        }

        return string.Join(" • ", parts);
    }
}