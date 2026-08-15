using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Parsing;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Ranking;

public static class GameRanker
{
    public static IReadOnlyList<BridgeGame> Rank(
        IEnumerable<BridgeGame> games,
        ParsedHltbQuery query
    )
    {
        ArgumentNullException.ThrowIfNull(games);
        ArgumentNullException.ThrowIfNull(query);

        if (query.IsIdLookup)
        {
            return games.ToList();
        }

        return games
            .Select((game, index) => new RankedGame(game, CalculateScore(game, query), index))
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Game.Similarity)
            .ThenBy(item => item.SourceIndex)
            .Select(item => item.Game)
            .ToList();
    }

    public static double CalculateScore(BridgeGame game, ParsedHltbQuery query)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(query);

        var search = TitleNormalizer.Normalize(query.SearchText);

        var name = TitleNormalizer.Normalize(game.Name);

        var alias = TitleNormalizer.Normalize(game.Alias);

        var score = 0.0;

        if (search.Length > 0 && name == search)
        {
            score += 1000;
        }
        else if (search.Length > 0 && alias == search)
        {
            score += 900;
        }

        if (
            search.Length > 0
            && name.StartsWith(search, StringComparison.Ordinal)
            && name != search
        )
        {
            score += 300;
        }

        if (
            search.Length > 0
            && alias.StartsWith(search, StringComparison.Ordinal)
            && alias != search
        )
        {
            score += 250;
        }

        var queryTokens = TitleNormalizer.Tokens(query.SearchText);

        if (queryTokens.Length > 0 && ContainsAllTokens(name, queryTokens))
        {
            score += 200;
        }

        if (queryTokens.Length > 0 && alias.Length > 0 && ContainsAllTokens(alias, queryTokens))
        {
            score += 175;
        }

        score += Math.Clamp(game.Similarity, 0, 1) * 300;

        if (query.Year.HasValue)
        {
            if (game.ReleaseYear == query.Year)
            {
                score += 700;
            }
            else if (game.ReleaseYear.HasValue)
            {
                score -= 350;
            }
        }

        if (query.Platform is not null)
        {
            if (PlatformNormalizer.Matches(query.Platform, game.Platforms))
            {
                score += 250;
            }
            else if (game.Platforms.Length > 0)
            {
                score -= 100;
            }
        }

        score += game.Type?.ToLowerInvariant() switch
        {
            "game" => 40,
            "multi" => 30,
            "dlc" => 0,
            "mod" => -40,
            _ => 0,
        };

        return score;
    }

    private static bool ContainsAllTokens(string candidate, IEnumerable<string> queryTokens)
    {
        var candidateTokens = candidate
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        return queryTokens.All(candidateTokens.Contains);
    }

    private sealed record RankedGame(BridgeGame Game, double Score, int SourceIndex);
}
