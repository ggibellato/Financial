namespace Financial.Application.DTOs;

public class CreditUpdateDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string AssetName { get; set; }
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public required string Type { get; set; }
    public decimal Value { get; set; }
}
