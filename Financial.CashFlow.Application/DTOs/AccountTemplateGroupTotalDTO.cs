namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountTemplateGroupTotalDTO
{
    public required string Currency { get; init; }
    public required decimal Total { get; init; }
    public decimal? CashbackTotal { get; init; }
    public decimal? OpenStatementTotal { get; init; }
    public decimal? NextStatementPotLinkedTotal { get; init; }
    public decimal? PotLinkedOwedTotal { get; init; }
}
