namespace Financial.CashFlow.Application.DTOs;

public sealed class MonthTransactionDTO
{
    public required Guid? AccountId { get; init; }
    public required string? AccountName { get; init; }
    public required string? AccountCurrency { get; init; }
    public required bool AccountIsActive { get; init; }
    public required AccountTransactionDTO Transaction { get; init; }
}
