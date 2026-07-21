namespace Financial.Investment.Application.DTOs;

public sealed class AssetBreakdownItemDTO
{
    public string AssetName { get; init; } = string.Empty;
    public decimal TotalInvested { get; init; }
}
