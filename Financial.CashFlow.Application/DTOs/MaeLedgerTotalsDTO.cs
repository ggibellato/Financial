namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for the all-time BRL/GBP totals across every Controle Mae ledger entry, regardless of any date filter applied to the entry list.
/// </summary>
public sealed class MaeLedgerTotalsDTO
{
    public required decimal TotalBrlValue { get; init; }
    public required decimal TotalGbpValue { get; init; }
}
