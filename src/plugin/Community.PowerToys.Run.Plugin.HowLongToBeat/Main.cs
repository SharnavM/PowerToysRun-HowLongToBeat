using System.Diagnostics;

using Community.PowerToys.Run.Plugin.HowLongToBeat.Bridge;
using Community.PowerToys.Run.Plugin.HowLongToBeat.Formatting;

using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat;

public sealed class Main :
    IPlugin,
    IDelayedExecutionPlugin,
    IDisposable
{
    private const int MinimumSearchLength = 2;
    private const int MaximumResults = 8;

    private IBridgeClient? _bridge;

    private readonly object _searchLock = new();

    private CancellationTokenSource?
        _activeSearchCancellation;

    private long _queryGeneration;
    private bool _disposed;

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
            BridgeClient.CreateDefault(
                pluginDirectory);

        // Creating BridgeClient does not start Python.
        // The process still starts lazily on first request.
    }

    public List<Result> Query(Query query)
    {
        ArgumentNullException.ThrowIfNull(query);

        MarkQueryChanged();

        return CreateImmediateResults(
            query.Search.Trim());
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
        var search =
            query.Search.Trim();

        if (search.Length < MinimumSearchLength)
        {
            return [];
        }

        var generation =
            Volatile.Read(ref _queryGeneration);

        var cancellation =
            BeginSearch();

        try
        {
        var bridge =
            _bridge
            ?? throw new BridgeException(
                "bridge_not_initialized",
                "HLTB bridge was not initialized.");

        var response =
            bridge
                .SearchAsync(
                    search,
                    BridgeSearchMode.All,
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

            var games =
                response.Results
                ?? [];

            if (games.Length == 0)
            {
                return
                [
                    CreateInfoResult(
                        "No HowLongToBeat results",
                        $"No matches found for \"{search}\".",
                        search),
                ];
            }

            return games
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
                    search),
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
                    search),
            ];
        }
        finally
        {
            EndSearch(cancellation);
        }
    }

    private List<Result> CreateImmediateResults(
        string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return
            [
                CreateInfoResult(
                    "Search HowLongToBeat",
                    "Type a game name after hltb.",
                    string.Empty),
            ];
        }

        if (search.Length < MinimumSearchLength)
        {
            return
            [
                CreateInfoResult(
                    "Keep typing",
                    "Enter at least 2 characters.",
                    search),
            ];
        }

        return
        [
            CreateInfoResult(
                $"Searching HowLongToBeat for \"{search}\"",
                "Results will appear after you stop typing.",
                search),
        ];
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