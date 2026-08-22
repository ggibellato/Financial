namespace Financial.CashFlow.Application.DTOs;

public sealed class MaeLedgerEntryDTO
{
    public required Guid Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public required string Note { get; init; }
    public required string SourceCurrency { get; init; }
    public decimal? BrlValue { get; init; }
    public decimal? GbpValue { get; init; }
}
