namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

public interface IBridgeClient : IDisposable
{
    Task<BridgePingResult> PingAsync(
        CancellationToken cancellationToken = default);

    Task<BridgeSearchResult> SearchAsync(
        string query,
        BridgeSearchMode mode = BridgeSearchMode.All,
        CancellationToken cancellationToken = default);

    Task<BridgeGame?> GetByIdAsync(
        int gameId,
        CancellationToken cancellationToken = default);

    Task ShutdownAsync(
        CancellationToken cancellationToken = default);
}