namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountGroupSummaryDTO
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required IReadOnlyList<AccountGroupMemberDTO> Members { get; init; }
    public required IReadOnlyList<CurrencyTotalDTO> Totals { get; init; }
    public required decimal CategoryTotalsSum { get; init; }
    public required IReadOnlyList<IncomeSourceTotalDTO> IncomeTotals { get; init; }
    public required decimal TotalIncoming { get; init; }
}
