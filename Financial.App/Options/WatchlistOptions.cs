using System.Collections.Generic;

namespace Financial.Presentation.App.Options;

internal static class WatchlistOptions
{
    internal static IReadOnlyList<WatchlistItem> DefaultDividendWatchlist { get; } = new[]
    {
        new WatchlistItem { Group = "Ja possuidas", Name = "KLBN4" },
        new WatchlistItem { Group = "Ja possuidas", Name = "TASA4" },
        new WatchlistItem { Group = "Ja possuidas", Name = "TAEE3" },
        new WatchlistItem { Group = "Outras Barse", Name = "UNIP6" },
        new WatchlistItem { Group = "Outras Barse", Name = "CMIG4" },
        new WatchlistItem { Group = "Outras Barse", Name = "TRPL4" },
        new WatchlistItem { Group = "Outras Barse", Name = "BBAS3" },
        new WatchlistItem { Group = "Outras",       Name = "CSAN3" },
    };
}
