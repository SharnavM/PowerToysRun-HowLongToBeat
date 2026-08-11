namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

public sealed class BridgeException : Exception
{
    public BridgeException(
        string code,
        string message)
        : base(message)
    {
        Code = code;
    }

    public BridgeException(
        string code,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}