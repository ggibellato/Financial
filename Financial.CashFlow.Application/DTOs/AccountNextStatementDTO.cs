namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountNextStatementDTO
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal OutstandingTotal { get; init; }
}
