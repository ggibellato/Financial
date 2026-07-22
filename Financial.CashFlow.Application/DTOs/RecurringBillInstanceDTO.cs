namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a recurring bill instance, joined with its template's display fields.
/// </summary>
public sealed class RecurringBillInstanceDTO
{
    public required Guid Id { get; init; }
    public required Guid TemplateId { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required int DueDay { get; init; }
    public required string Description { get; init; }
    public required string Area { get; init; }
    public required string Note { get; init; }
    public string? NitNumber { get; init; }
    public decimal? MinimumWageValue { get; init; }
    public required decimal Value { get; init; }
    public required string Status { get; init; }
}
