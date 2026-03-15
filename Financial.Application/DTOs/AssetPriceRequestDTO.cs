namespace Financial.Application.DTOs;

public class AssetPriceRequestDTO
{
    public required string Exchange { get; set; }
    public required string Ticker { get; set; }
}
