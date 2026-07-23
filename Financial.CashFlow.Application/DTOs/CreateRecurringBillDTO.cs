namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to create a new recurring bill.
/// </summary>
public sealed class CreateRecurringBillDTO
{
    public required int DueDay { get; init; }
    public required string Description { get; init; }
    public required decimal Value { get; init; }
    public required string Area { get; init; }
    public string Note { get; init; } = string.Empty;
}
