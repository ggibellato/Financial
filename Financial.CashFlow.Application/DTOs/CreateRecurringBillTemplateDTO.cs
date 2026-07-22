namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to create a new recurring bill template.
/// </summary>
public sealed class CreateRecurringBillTemplateDTO
{
    public required int DueDay { get; init; }
    public required string Description { get; init; }
    public required decimal Value { get; init; }
    public required string Area { get; init; }
    public string Note { get; init; } = string.Empty;
    public string? NitNumber { get; init; }
    public decimal? MinimumWageValue { get; init; }
}
