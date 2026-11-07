namespace Financial.CashFlow.Application.DTOs;

public sealed class SettlementEntryUpdateDTO
{
    public required DateOnly Date { get; init; }
    public required string Description { get; init; }
    public string? Note { get; init; }
    public required string PaidBy { get; init; }
    public required decimal Amount { get; init; }
    public decimal? ConvertedValue { get; init; }
}
