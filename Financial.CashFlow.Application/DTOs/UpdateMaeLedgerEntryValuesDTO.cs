namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to manually override a Controle Mae ledger entry's currency values.
/// </summary>
public sealed class UpdateMaeLedgerEntryValuesDTO
{
    public decimal? BrlValue { get; init; }
    public decimal? GbpValue { get; init; }
}
