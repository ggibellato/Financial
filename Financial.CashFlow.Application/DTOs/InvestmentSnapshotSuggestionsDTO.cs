namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentSnapshotSuggestionsDTO
{
    public required IReadOnlyList<InvestmentSnapshotSuggestionDTO> Suggestions { get; init; }
    public required IReadOnlyList<InvestmentSnapshotSuggestionSkippedDTO> NotUpdated { get; init; }
}
