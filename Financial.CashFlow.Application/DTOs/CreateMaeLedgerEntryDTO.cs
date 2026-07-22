namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to create a new Controle Mae ledger entry.
/// </summary>
public sealed class CreateMaeLedgerEntryDTO
{
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public string Note { get; init; } = string.Empty;
    public required string SourceCurrency { get; init; }
    public required decimal SourceValue { get; init; }
}
