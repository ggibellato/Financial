namespace Financial.CashFlow.Application.DTOs;

public sealed class SettlementEntryDTO
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public required string Note { get; init; }
    public required string PaidBy { get; init; }
    public required string Currency { get; init; }
    public required decimal Amount { get; init; }
    public string? ConvertedCurrency { get; init; }
    public decimal? ConvertedValue { get; init; }
    public string? Warning { get; init; }
}
