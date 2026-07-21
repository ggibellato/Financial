namespace Financial.Investment.Application.DTOs;

public sealed class PortfolioBreakdownItemDTO
{
    public string PortfolioName { get; init; } = string.Empty;
    public decimal TotalInvested { get; init; }
    public IReadOnlyList<AssetBreakdownItemDTO> Assets { get; init; } = [];
}
