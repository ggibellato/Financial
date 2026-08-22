namespace Financial.Investment.Application.DTOs;

public class BrokerNodeDTO
{
    public required string Name { get; set; }

    public required string Currency { get; set; }

    public int PortfolioCount { get; set; }

    public int TotalAssets { get; set; }

    public List<PortfolioNodeDTO> Portfolios { get; set; } = new();
}

