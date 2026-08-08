using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat;

public sealed class Main : IPlugin
{
    public static string PluginID =>
        "0ADE2E6A74FF4E1AADBAABE8F06FFCD0";

    public string Name =>
        "HowLongToBeat";

    public string Description =>
        "Search game completion times on HowLongToBeat.";

    public void Init(PluginInitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
    }

    public List<Result> Query(Query query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var search = query.Search.Trim();

        var subtitle = string.IsNullOrEmpty(search)
            ? "Type a game name after hltb."
            : $"Received: {search}";

        return
        [
            new Result
            {
                Title = "HowLongToBeat plugin is running",
                SubTitle = subtitle,
                QueryTextDisplay = search,
                IcoPath = "Images\\howlongtobeat.dark.png",
                Score = 100,
                DisableUsageBasedScoring = true,
                Action = _ => true,
            },
        ];
    }
}