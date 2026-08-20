namespace Financial.Investment.Application.DTOs;

public class AssetPriceDTO
{
    public required string Exchange { get; set; }
    public required string Ticker { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    /// <summary>When the quote was taken, for a live fetch. Null for a stored price, which has
    /// no time of day - see <see cref="AsOfDate"/>.</summary>
    public DateTimeOffset? AsOf { get; set; }

    /// <summary>The date of the Price History entry this price came from. Set only when the value
    /// was read from history rather than fetched. Kept as a date rather than folded into
    /// <see cref="AsOf"/> at midnight, because a timestamp would be re-interpreted against the
    /// reader's time zone and could show the previous day.</summary>
    public DateOnly? AsOfDate { get; set; }

    public bool IsManual { get; set; }
}
