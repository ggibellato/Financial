namespace Financial.Api.Options;

public sealed class WatchlistOptions
{
    public const string SectionName = "Watchlist";
    public List<WatchlistItem> Items { get; set; } = [];
}
