using System.Diagnostics;
using System.Windows.Controls;

using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Formatting;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Parsing;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Ranking;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Caching;
using Microsoft.PowerToys.Settings.UI.Library;

using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat;

public sealed class Main :
    IPlugin,
    IDelayedExecutionPlugin,
    ISettingProvider,
    IDisposable
{
    private const int MinimumSearchLength = 2;
    private const int MaximumResults = 8;
    private const string BridgeIdleTimeoutOptionKey =
        "BridgeIdleTimeoutMinutes";
    private const int
        DefaultBridgeIdleTimeoutMinutes = 10;
    private const int
        MaximumBridgeIdleTimeoutMinutes = 120;

    private IBridgeClient? _bridge;

    private readonly object _searchLock = new();
    private readonly SearchCache _searchCache =
        new();

    private CancellationTokenSource?
        _activeSearchCancellation;

    private long _queryGeneration;
    private bool _disposed;
    private int _bridgeIdleTimeoutMinutes =
    DefaultBridgeIdleTimeoutMinutes;

        public Main()
    {
    }

    public Main(IBridgeClient bridge)
    {
        _bridge =
            bridge
            ?? throw new ArgumentNullException(
                nameof(bridge));
    }

    public static string PluginID =>
        "0ADE2E6A74FF4E1AADBAABE8F06FFCD0";

    public string Name =>
        "HowLongToBeat";

    public string Description =>
        "Search game completion times on HowLongToBeat.";

    public IEnumerable<PluginAdditionalOption>
        AdditionalOptions =>
    [
        new PluginAdditionalOption
        {
            Key =
                BridgeIdleTimeoutOptionKey,

            DisplayLabel =
                "Bridge idle shutdown (minutes)",

            DisplayDescription =
                "Automatically stop the HowLongToBeat "
                + "helper process after this many minutes "
                + "without bridge activity. Set 0 to keep "
                + "it running until PowerToys exits.",

            PluginOptionType =
                PluginAdditionalOption
                    .AdditionalOptionType
                    .Numberbox,

            NumberValue =
                DefaultBridgeIdleTimeoutMinutes,

            NumberBoxMin = 0,

            NumberBoxMax =
                MaximumBridgeIdleTimeoutMinutes,

            NumberBoxSmallChange = 1,

            NumberBoxLargeChange = 10,
        },
    ];

    public void Init(PluginInitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var pluginDirectory =
            context.CurrentPluginMetadata
                ?.PluginDirectory;

        if (string.IsNullOrWhiteSpace(
                pluginDirectory))
        {
            throw new InvalidOperationException(
                "PowerToys did not provide the plugin directory.");
        }

        _bridge ??=
            new IdleShutdownBridgeClient(
                BridgeClient.CreateDefault(
                    pluginDirectory),
                ToIdleTimeout(
                    _bridgeIdleTimeoutMinutes));

        // Creating BridgeClient does not start Python.
        // The process still starts lazily on first request.
    }

    public List<Result> Query(Query query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rawQuery =
            query.Search.Trim();

        MarkQueryChanged();

        var parsed =
            HltbQueryParser.Parse(
                rawQuery);

        return CreateImmediateResults(
            rawQuery,
            parsed);
    }

    public List<Result> Query(
        Query query,
        bool delayedExecution)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!delayedExecution)
        {
            return Query(query);
        }

        return ExecuteDelayedQuery(query);
    }

    private List<Result> ExecuteDelayedQuery(
    Query query)
{
        var rawQuery =
            query.Search.Trim();

        var parsed =
            HltbQueryParser.Parse(
                rawQuery);

        if (!parsed.IsValid)
        {
            return
            [
                CreateInfoResult(
                    parsed.ErrorTitle!,
                    parsed.ErrorMessage!,
                    rawQuery),
            ];
        }

        if (
            !parsed.IsIdLookup
            && (
                string.IsNullOrWhiteSpace(
                    parsed.SearchText)
                || parsed.SearchText.Length <
                    MinimumSearchLength
            ))
        {
            return [];
        }

        var generation =
            Volatile.Read(
                ref _queryGeneration);

        var cancellation =
            BeginSearch();

        try
        {
            var bridge =
                _bridge
                ?? throw new BridgeException(
                    "bridge_not_initialized",
                    "HLTB bridge was not initialized.");

            if (parsed.IsIdLookup)
            {
                return ExecuteIdLookup(
                    bridge,
                    parsed,
                    rawQuery,
                    generation,
                    cancellation);
            }

            BridgeSearchResult response;

            if (
                !_searchCache.TryGet(
                    parsed.SearchText!,
                    parsed.SearchMode,
                    out response))
            {
                response =
                    bridge
                        .SearchAsync(
                            parsed.SearchText!,
                            parsed.SearchMode,
                            cancellation.Token)
                        .GetAwaiter()
                        .GetResult();

                if (
                    !cancellation.IsCancellationRequested
                    && generation ==
                        Volatile.Read(
                            ref _queryGeneration))
                {
                    _searchCache.Set(
                        parsed.SearchText!,
                        parsed.SearchMode,
                        response);
                }
            }

            if (
                cancellation.IsCancellationRequested
                || generation !=
                    Volatile.Read(
                        ref _queryGeneration))
            {
                return [];
            }

            var games =
                response.Results
                ?? [];

            if (games.Length == 0)
            {
                return
                [
                    CreateInfoResult(
                        "No HowLongToBeat results",
                        $"No matches found for \"{parsed.SearchText}\".",
                        rawQuery),
                ];
            }

            var ranked =
                GameRanker.Rank(
                    games,
                    parsed);

            return ranked
                .Take(MaximumResults)
                .Select(
                    (game, index) =>
                        CreateGameResult(
                            game,
                            index))
                .ToList();
        }
        catch (OperationCanceledException)
        {
            return [];
        }
        catch (BridgeException exception)
        {
            if (
                cancellation.IsCancellationRequested
                || generation !=
                    Volatile.Read(
                        ref _queryGeneration))
            {
                return [];
            }

            return
            [
                CreateBridgeErrorResult(
                    exception,
                    rawQuery),
            ];
        }
        catch (Exception)
        {
            if (
                cancellation.IsCancellationRequested
                || generation !=
                    Volatile.Read(
                        ref _queryGeneration))
            {
                return [];
            }

            return
            [
                CreateInfoResult(
                    "HowLongToBeat search failed",
                    "An unexpected search error occurred.",
                    rawQuery),
            ];
        }
        finally
        {
            EndSearch(cancellation);
        }
    }

    private List<Result> CreateImmediateResults(
    string rawQuery,
    ParsedHltbQuery parsed)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            return
            [
                CreateInfoResult(
                    "Search HowLongToBeat",
                    "Type a game name after hltb.",
                    string.Empty),
            ];
        }

        if (!parsed.IsValid)
        {
            return
            [
                CreateInfoResult(
                    parsed.ErrorTitle!,
                    parsed.ErrorMessage!,
                    rawQuery),
            ];
        }

        if (parsed.IsIdLookup)
        {
            return
            [
                CreateInfoResult(
                    "Looking up HowLongToBeat game…",
                    $"Game ID {parsed.GameId}",
                    rawQuery),
            ];
        }

        var search =
            parsed.SearchText!;

        if (search.Length < MinimumSearchLength)
        {
            return
            [
                CreateInfoResult(
                    "Keep typing",
                    "Enter at least 2 characters.",
                    rawQuery),
            ];
        }

        return
        [
            CreateInfoResult(
                "Searching HowLongToBeat…",
                BuildSearchDescription(parsed),
                rawQuery),
        ];
    }

    private static string BuildSearchDescription(
    ParsedHltbQuery parsed)
    {
        var parts =
            new List<string>
            {
                $"Looking up \"{parsed.SearchText}\"",
            };

        if (parsed.Year.HasValue)
        {
            parts.Add(
                $"Year {parsed.Year}");
        }

        if (parsed.Platform is not null)
        {
            parts.Add(
                parsed.Platform);
        }

        switch (parsed.SearchMode)
        {
            case BridgeSearchMode.DlcOnly:
                parts.Add("DLC only");
                break;

            case BridgeSearchMode.HideDlc:
                parts.Add("No DLC/mods");
                break;
        }

        return string.Join(
            " • ",
            parts);
    }

    private static Result CreateGameResult(
        BridgeGame game,
        int index)
    {
        return new Result
        {
            Title =
                GameResultFormatter.BuildTitle(game),

            SubTitle =
                GameResultFormatter.BuildSubtitle(game),

            QueryTextDisplay =
                game.Name,

            IcoPath =
                "Images\\howlongtobeat.dark.png",

            Score =
                1000 - index,

            DisableUsageBasedScoring =
                true,

            ContextData =
                game,

            Action =
                _ => OpenUrl(game.Url),
        };
    }

    private static Result CreateInfoResult(
        string title,
        string subtitle,
        string queryText)
    {
        return new Result
        {
            Title = title,
            SubTitle = subtitle,

            QueryTextDisplay =
                queryText,

            IcoPath =
                "Images\\howlongtobeat.dark.png",

            Score = 100,

            DisableUsageBasedScoring =
                true,

            Action =
                _ => false,
        };
    }

    private static Result CreateBridgeErrorResult(
        BridgeException exception,
        string search)
    {
        return exception.Code switch
        {
            "timeout" =>
                CreateInfoResult(
                    "HowLongToBeat search timed out",
                    "Try the search again.",
                    search),

            "bridge_not_found" =>
                CreateInfoResult(
                    "HLTB bridge is missing",
                    "Rebuild or reinstall the plugin.",
                    search),

            "upstream_error" =>
                CreateInfoResult(
                    "HowLongToBeat is unavailable",
                    "The HLTB service returned an error.",
                    search),

            _ =>
                CreateInfoResult(
                    "HowLongToBeat search failed",
                    exception.Message,
                    search),
        };
    }

    private static bool OpenUrl(string? value)
    {
        if (
            !Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri)
            || (
                !string.Equals(
                    uri.Scheme,
                    Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase)
                && !string.Equals(
                    uri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase)
            ))
        {
            return false;
        }

        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        uri.AbsoluteUri,

                    UseShellExecute =
                        true,
                });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void MarkQueryChanged()
    {
        Interlocked.Increment(
            ref _queryGeneration);

        lock (_searchLock)
        {
            _activeSearchCancellation
                ?.Cancel();
        }
    }

    private CancellationTokenSource BeginSearch()
    {
        lock (_searchLock)
        {
            _activeSearchCancellation
                ?.Cancel();

            var cancellation =
                new CancellationTokenSource();

            _activeSearchCancellation =
                cancellation;

            return cancellation;
        }
    }

    private List<Result> ExecuteIdLookup(
    IBridgeClient bridge,
    ParsedHltbQuery parsed,
    string rawQuery,
    long generation,
    CancellationTokenSource cancellation)
    {
        var game =
            bridge
                .GetByIdAsync(
                    parsed.GameId!.Value,
                    cancellation.Token)
                .GetAwaiter()
                .GetResult();

        if (
            cancellation.IsCancellationRequested
            || generation !=
                Volatile.Read(
                    ref _queryGeneration))
        {
            return [];
        }

        if (game is null)
        {
            return
            [
                CreateInfoResult(
                    "HowLongToBeat game not found",
                    $"No game found for ID {parsed.GameId}.",
                    rawQuery),
            ];
        }

        return
        [
            CreateGameResult(
                game,
                0),
        ];
    }

    private void EndSearch(
        CancellationTokenSource cancellation)
    {
        lock (_searchLock)
        {
            if (ReferenceEquals(
                    _activeSearchCancellation,
                    cancellation))
            {
                _activeSearchCancellation =
                    null;
            }
        }

        cancellation.Dispose();
    }

    public void UpdateSettings(
        PowerLauncherPluginSettings settings)
    {
        var minutes =
            DefaultBridgeIdleTimeoutMinutes;

        var option =
            settings?
                .AdditionalOptions?
                .FirstOrDefault(
                    item =>
                        item.Key ==
                        BridgeIdleTimeoutOptionKey);

        if (option is not null)
        {
            minutes =
                Math.Clamp(
                    (int)Math.Round(
                        option.NumberValue,
                        MidpointRounding.AwayFromZero),
                    0,
                    MaximumBridgeIdleTimeoutMinutes);
        }

        _bridgeIdleTimeoutMinutes =
            minutes;

        if (_bridge is
            IdleShutdownBridgeClient idleBridge)
        {
            idleBridge.SetIdleTimeout(
                ToIdleTimeout(minutes));
        }
    }

    public Control CreateSettingPanel()
    {
        throw new NotImplementedException();
    }

    private static TimeSpan ToIdleTimeout(
        int minutes)
    {
        return minutes == 0
            ? TimeSpan.Zero
            : TimeSpan.FromMinutes(minutes);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        lock (_searchLock)
        {
            _activeSearchCancellation
                ?.Cancel();
        }

        _bridge?.Dispose();
    }
}