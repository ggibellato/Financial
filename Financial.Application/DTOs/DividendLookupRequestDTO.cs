namespace Financial.Application.DTOs;

public class DividendLookupRequestDTO
{
    public required string Exchange { get; set; }
    public required string Ticker { get; set; }
}
