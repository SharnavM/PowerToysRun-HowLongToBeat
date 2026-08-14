using System.Collections.Concurrent;

using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Ranking;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Caching;

public sealed class SearchCache
{
    private const int MaximumEntries = 128;

    private readonly ConcurrentDictionary<
        CacheKey,
        CacheEntry> _entries =
        new();

    private readonly TimeSpan _successTtl;
    private readonly TimeSpan _emptyTtl;

    private readonly Func<DateTimeOffset> _clock;

    public SearchCache(
        TimeSpan? successTtl = null,
        TimeSpan? emptyTtl = null,
        Func<DateTimeOffset>? clock = null)
    {
        _successTtl =
            successTtl
            ?? TimeSpan.FromMinutes(15);

        _emptyTtl =
            emptyTtl
            ?? TimeSpan.FromMinutes(2);

        _clock =
            clock
            ?? (() => DateTimeOffset.UtcNow);
    }

    public bool TryGet(
        string query,
        BridgeSearchMode mode,
        out BridgeSearchResult result)
    {
        var key =
            CreateKey(
                query,
                mode);

        if (!_entries.TryGetValue(
                key,
                out var entry))
        {
            result = default!;
            return false;
        }

        if (entry.ExpiresAt <= _clock())
        {
            _entries.TryRemove(
                key,
                out _);

            result = default!;
            return false;
        }

        result = entry.Result;
        return true;
    }

    public void Set(
        string query,
        BridgeSearchMode mode,
        BridgeSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(
            result);

        var now =
            _clock();

        var ttl =
            result.Results.Length == 0
                ? _emptyTtl
                : _successTtl;

        var key =
            CreateKey(
                query,
                mode);

        _entries[key] =
            new CacheEntry(
                result,
                now,
                now + ttl);

        TrimIfNeeded();
    }

    public void Clear()
    {
        _entries.Clear();
    }

    private static CacheKey CreateKey(
        string query,
        BridgeSearchMode mode)
    {
        return new CacheKey(
            TitleNormalizer.Normalize(
                query),
            mode);
    }

    private void TrimIfNeeded()
    {
        var excess =
            _entries.Count
            - MaximumEntries;

        if (excess <= 0)
        {
            return;
        }

        var oldest =
            _entries
                .OrderBy(
                    item =>
                        item.Value.StoredAt)
                .Take(excess)
                .Select(
                    item =>
                        item.Key)
                .ToArray();

        foreach (var key in oldest)
        {
            _entries.TryRemove(
                key,
                out _);
        }
    }

    private sealed record CacheKey(
        string Query,
        BridgeSearchMode Mode);

    private sealed record CacheEntry(
        BridgeSearchResult Result,
        DateTimeOffset StoredAt,
        DateTimeOffset ExpiresAt);
}