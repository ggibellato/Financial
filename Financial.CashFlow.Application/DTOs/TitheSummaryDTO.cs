namespace Financial.CashFlow.Application.DTOs;

public sealed class TitheSummaryDTO
{
    public required decimal IncomeBase { get; init; }
    public required decimal CalculatedTithe { get; init; }
    public required decimal TithePaid { get; init; }
    public required decimal TitheBalance { get; init; }
    public TitheCarryForwardDTO? CarryForward { get; init; }
}
