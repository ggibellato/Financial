namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentSnapshotSuggestionDTO
{
    public required Guid SnapshotId { get; init; }
    public required Guid AccountId { get; init; }
    public required string AccountName { get; init; }
    public required decimal CurrentValue { get; init; }
    public required decimal SuggestedValue { get; init; }
    public required string SourceDescription { get; init; }
}
