namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentSnapshotSuggestionSkippedDTO
{
    public required Guid AccountId { get; init; }
    public required string AccountName { get; init; }
    public required string Reason { get; init; }
}
