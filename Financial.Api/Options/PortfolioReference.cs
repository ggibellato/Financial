namespace Financial.Api.Options;

public sealed class PortfolioReference
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
}
