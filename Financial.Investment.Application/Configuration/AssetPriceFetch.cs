namespace Financial.Investment.Application.Configuration;

public sealed class AssetPriceFetch
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
}
