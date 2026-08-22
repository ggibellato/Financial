namespace Financial.Investment.Application.DTOs;

public class PortfolioNodeDTO
{
    public required string Name { get; set; }

    public int AssetCount { get; set; }

    public List<AssetNodeDTO> Assets { get; set; } = new();
}

