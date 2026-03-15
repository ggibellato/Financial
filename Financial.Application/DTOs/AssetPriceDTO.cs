namespace Financial.Application.DTOs;

public class AssetPriceDTO
{
    public required string Exchange { get; set; }
    public required string Ticker { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTimeOffset? AsOf { get; set; }
}
