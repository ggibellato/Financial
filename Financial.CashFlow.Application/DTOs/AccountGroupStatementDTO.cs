namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountGroupStatementDTO
{
    public required decimal OutstandingTotal { get; init; }
    public required bool IsPaid { get; init; }
}
