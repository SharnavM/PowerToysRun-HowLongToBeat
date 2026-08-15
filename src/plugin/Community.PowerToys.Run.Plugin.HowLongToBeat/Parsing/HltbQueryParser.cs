using System.Text;
using System.Text.RegularExpressions;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Parsing;

public static partial class HltbQueryParser
{
    [GeneratedRegex(@"^id:(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GameIdRegex();

    public static ParsedHltbQuery Parse(string? input)
    {
        var value = input?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            return new ParsedHltbQuery(
                SearchText: string.Empty,
                GameId: null,
                Year: null,
                Platform: null,
                SearchMode: BridgeSearchMode.All
            );
        }

        var tokenization = Tokenize(value);

        if (!tokenization.Success)
        {
            return ParsedHltbQuery.Invalid("Invalid query", tokenization.ErrorMessage!);
        }

        var tokens = tokenization.Tokens!;

        var titleParts = new List<string>();

        int? year = null;
        string? platform = null;

        var mode = BridgeSearchMode.All;

        var modeWasSpecified = false;

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            if (token.Equals("--dlc", StringComparison.OrdinalIgnoreCase))
            {
                if (modeWasSpecified && mode != BridgeSearchMode.DlcOnly)
                {
                    return ConflictingDlcOptions();
                }

                mode = BridgeSearchMode.DlcOnly;

                modeWasSpecified = true;
                continue;
            }

            if (token.Equals("--no-dlc", StringComparison.OrdinalIgnoreCase))
            {
                if (modeWasSpecified && mode != BridgeSearchMode.HideDlc)
                {
                    return ConflictingDlcOptions();
                }

                mode = BridgeSearchMode.HideDlc;

                modeWasSpecified = true;
                continue;
            }

            if (token.StartsWith("--year=", StringComparison.OrdinalIgnoreCase))
            {
                if (year.HasValue)
                {
                    return ParsedHltbQuery.Invalid("Duplicate year", "Specify --year only once.");
                }

                var rawYear = token["--year=".Length..];

                var parsed = ParseYear(rawYear);

                if (!parsed.Success)
                {
                    return parsed.Error!;
                }

                year = parsed.Year;
                continue;
            }

            if (token.Equals("--year", StringComparison.OrdinalIgnoreCase))
            {
                if (year.HasValue)
                {
                    return ParsedHltbQuery.Invalid("Duplicate year", "Specify --year only once.");
                }

                if (
                    index + 1 >= tokens.Count
                    || tokens[index + 1].StartsWith("--", StringComparison.Ordinal)
                )
                {
                    return ParsedHltbQuery.Invalid("Missing year", "Example: --year 2023");
                }

                var parsed = ParseYear(tokens[++index]);

                if (!parsed.Success)
                {
                    return parsed.Error!;
                }

                year = parsed.Year;
                continue;
            }

            if (token.StartsWith("--platform=", StringComparison.OrdinalIgnoreCase))
            {
                if (platform is not null)
                {
                    return ParsedHltbQuery.Invalid(
                        "Duplicate platform",
                        "Specify --platform only once."
                    );
                }

                var raw = token["--platform=".Length..];

                if (string.IsNullOrWhiteSpace(raw))
                {
                    return MissingPlatform();
                }

                platform = PlatformNormalizer.Normalize(raw);

                continue;
            }

            if (token.Equals("--platform", StringComparison.OrdinalIgnoreCase))
            {
                if (platform is not null)
                {
                    return ParsedHltbQuery.Invalid(
                        "Duplicate platform",
                        "Specify --platform only once."
                    );
                }

                if (
                    index + 1 >= tokens.Count
                    || tokens[index + 1].StartsWith("--", StringComparison.Ordinal)
                )
                {
                    return MissingPlatform();
                }

                platform = PlatformNormalizer.Normalize(tokens[++index]);

                continue;
            }

            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                return ParsedHltbQuery.Invalid("Unknown option", $"Unknown option: {token}");
            }

            titleParts.Add(token);
        }

        var searchText = string.Join(" ", titleParts).Trim();

        if (searchText.Length == 0)
        {
            return ParsedHltbQuery.Invalid(
                "Missing game title",
                "Enter a game title before or after the options."
            );
        }

        var idMatch = GameIdRegex().Match(searchText);

        if (idMatch.Success)
        {
            if (year.HasValue || platform is not null || modeWasSpecified)
            {
                return ParsedHltbQuery.Invalid(
                    "Invalid ID lookup",
                    "Game ID lookup cannot be combined with search modifiers."
                );
            }

            if (!int.TryParse(idMatch.Groups[1].Value, out var gameId) || gameId <= 0)
            {
                return InvalidGameId();
            }

            return new ParsedHltbQuery(
                SearchText: null,
                GameId: gameId,
                Year: null,
                Platform: null,
                SearchMode: BridgeSearchMode.All
            );
        }

        if (searchText.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
        {
            return InvalidGameId();
        }

        return new ParsedHltbQuery(
            SearchText: searchText,
            GameId: null,
            Year: year,
            Platform: platform,
            SearchMode: mode
        );
    }

    private static (bool Success, int? Year, ParsedHltbQuery? Error) ParseYear(string value)
    {
        if (value.Length != 4 || !int.TryParse(value, out var year) || year is < 1900 or > 2999)
        {
            return (
                false,
                null,
                ParsedHltbQuery.Invalid("Invalid year", "Use --year followed by a four-digit year.")
            );
        }

        return (true, year, null);
    }

    private static ParsedHltbQuery ConflictingDlcOptions()
    {
        return ParsedHltbQuery.Invalid("Conflicting options", "Use either --dlc or --no-dlc.");
    }

    private static ParsedHltbQuery MissingPlatform()
    {
        return ParsedHltbQuery.Invalid("Missing platform", "Example: --platform PC");
    }

    private static ParsedHltbQuery InvalidGameId()
    {
        return ParsedHltbQuery.Invalid("Invalid HowLongToBeat ID", "Example: id:68151");
    }

    private static TokenizationResult Tokenize(string input)
    {
        var tokens = new List<string>();

        var current = new StringBuilder();

        var inQuotes = false;
        var quoteCharacter = '\0';

        void Flush()
        {
            if (current.Length == 0)
            {
                return;
            }

            tokens.Add(current.ToString());

            current.Clear();
        }

        foreach (var character in input)
        {
            if (inQuotes)
            {
                if (character == quoteCharacter)
                {
                    inQuotes = false;
                    continue;
                }

                current.Append(character);
                continue;
            }

            if (character == '"' || character == '\'')
            {
                inQuotes = true;
                quoteCharacter = character;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                Flush();
                continue;
            }

            current.Append(character);
        }

        if (inQuotes)
        {
            return new TokenizationResult(
                Success: false,
                Tokens: null,
                ErrorMessage: "A quoted value is missing its closing quote."
            );
        }

        Flush();

        return new TokenizationResult(Success: true, Tokens: tokens, ErrorMessage: null);
    }

    private sealed record TokenizationResult(
        bool Success,
        List<string>? Tokens,
        string? ErrorMessage
    );
}
