using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Parsing;

public sealed record ParsedHltbQuery(
    string? SearchText,
    int? GameId,
    int? Year,
    string? Platform,
    BridgeSearchMode SearchMode,
    string? ErrorTitle = null,
    string? ErrorMessage = null)
{
    public bool IsValid =>
        ErrorTitle is null;

    public bool IsIdLookup =>
        GameId.HasValue;

    public static ParsedHltbQuery Invalid(
        string title,
        string message)
    {
        return new ParsedHltbQuery(
            SearchText: null,
            GameId: null,
            Year: null,
            Platform: null,
            SearchMode: BridgeSearchMode.All,
            ErrorTitle: title,
            ErrorMessage: message);
    }
}