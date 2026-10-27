namespace Financial.CashFlow.Application.DTOs;

public sealed class MoveChargeToStatementDTO
{
    public required int Year { get; init; }
    public required int Month { get; init; }
}
