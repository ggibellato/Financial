namespace Financial.Presentation.App.Options;

public class WatchlistOptions
{
    public const string SectionName = "Watchlist";
    public List<WatchlistItem> Items { get; set; } = new();
}
