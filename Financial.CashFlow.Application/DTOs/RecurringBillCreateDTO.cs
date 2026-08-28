namespace Financial.CashFlow.Application.DTOs;

public sealed class RecurringBillCreateDTO
{
    public required int DueDay { get; init; }
    public required string Description { get; init; }
    public required decimal Value { get; init; }
    public required string Area { get; init; }
    public string Note { get; init; } = string.Empty;
}
