namespace Financial.Investment.Application.DTOs;

public class PortfolioCreateDTO
{
    public required string BrokerName { get; set; }

    public required string Name { get; set; }

    public string? ArcaBucket { get; set; }
}
