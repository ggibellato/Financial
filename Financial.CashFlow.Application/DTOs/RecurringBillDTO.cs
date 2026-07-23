namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a recurring bill.
/// </summary>
public sealed class RecurringBillDTO
{
    public required Guid Id { get; init; }
    public required int DueDay { get; init; }
    public required string Description { get; init; }
    public required decimal Value { get; init; }
    public required string Area { get; init; }
    public required string Note { get; init; }
    public string? NitNumber { get; init; }
    public decimal? MinimumWageValue { get; init; }
    public required string Status { get; init; }
}
