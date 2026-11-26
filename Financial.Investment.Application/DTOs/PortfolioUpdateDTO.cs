namespace Financial.Investment.Application.DTOs;

public class PortfolioUpdateDTO
{
    public required string Name { get; set; }

    public string? ArcaBucket { get; set; }
}
