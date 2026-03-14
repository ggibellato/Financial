namespace Financial.Application.DTOs;

public class CreditDeleteDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string AssetName { get; set; }
    public Guid Id { get; set; }
}
