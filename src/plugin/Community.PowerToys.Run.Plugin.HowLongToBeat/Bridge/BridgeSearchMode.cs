namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

public enum BridgeSearchMode
{
    All,
    HideDlc,
    DlcOnly,
}

internal static class BridgeSearchModeExtensions
{
    public static string ToProtocolValue(
        this BridgeSearchMode mode)
    {
        return mode switch
        {
            BridgeSearchMode.All => "all",
            BridgeSearchMode.HideDlc => "hide_dlc",
            BridgeSearchMode.DlcOnly => "dlc_only",

            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                null),
        };
    }
}