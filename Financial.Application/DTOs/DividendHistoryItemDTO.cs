namespace Financial.Application.DTOs;

public class DividendHistoryItemDTO
{
    public required string Type { get; set; }
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}
