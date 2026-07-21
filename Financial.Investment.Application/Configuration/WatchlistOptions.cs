namespace Financial.Investment.Application.Configuration;

public sealed class WatchlistOptions
{
    public const string SectionName = "Watchlist";
    public List<WatchlistItem> Items { get; set; } = [];
}
