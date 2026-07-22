namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update a recurring bill instance's status and value.
/// </summary>
public sealed class UpdateRecurringBillInstanceDTO
{
    public required string Status { get; init; }
    public required decimal Value { get; init; }
}
