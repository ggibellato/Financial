namespace Financial.CashFlow.Application.DTOs;

public sealed class UpdateRecurringBillDTO
{
    public required string Status { get; init; }
    public required decimal Value { get; init; }
}
