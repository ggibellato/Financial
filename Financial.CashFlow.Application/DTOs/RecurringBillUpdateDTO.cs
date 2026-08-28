namespace Financial.CashFlow.Application.DTOs;

public sealed class RecurringBillUpdateDTO
{
    public required string Status { get; init; }
    public required decimal Value { get; init; }
}
