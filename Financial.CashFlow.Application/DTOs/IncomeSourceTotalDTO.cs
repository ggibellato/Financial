namespace Financial.CashFlow.Application.DTOs;

public sealed class IncomeSourceTotalDTO
{
    public required Guid IncomeSourceId { get; init; }
    public required string Name { get; init; }
    public required decimal NetValue { get; init; }
    public decimal? GrossValue { get; init; }
}
