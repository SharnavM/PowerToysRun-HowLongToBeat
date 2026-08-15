using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;

public sealed record BridgePingResult(
    [property: JsonPropertyName("protocolVersion")] int ProtocolVersion,
    [property: JsonPropertyName("bridgeVersion")] string BridgeVersion
);

public sealed record BridgeGame(
    [property: JsonPropertyName("gameId")] int GameId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("alias")] string? Alias,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("releaseYear")] int? ReleaseYear,
    [property: JsonPropertyName("platforms")] string[] Platforms,
    [property: JsonPropertyName("mainSeconds")] int? MainSeconds,
    [property: JsonPropertyName("mainExtraSeconds")] int? MainExtraSeconds,
    [property: JsonPropertyName("completionistSeconds")] int? CompletionistSeconds,
    [property: JsonPropertyName("allStylesSeconds")] int? AllStylesSeconds,
    [property: JsonPropertyName("similarity")] double Similarity,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("imageUrl")] string? ImageUrl
);

public sealed record BridgeSearchResult(
    [property: JsonPropertyName("results")] BridgeGame[] Results,
    [property: JsonPropertyName("count")] int Count
);

public sealed record BridgeGameLookupResult([property: JsonPropertyName("game")] BridgeGame? Game);

public sealed record BridgeShutdownResult(
    [property: JsonPropertyName("shuttingDown")] bool ShuttingDown
);
