namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountTransactionStatementDTO
{
    public required Guid AccountId { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
}
