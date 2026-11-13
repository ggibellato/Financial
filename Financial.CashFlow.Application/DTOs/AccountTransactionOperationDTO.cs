namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountTransactionOperationDTO
{
    public required Guid OperationId { get; init; }
    public required string Kind { get; init; }
    public required Guid PotsAccountId { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public Guid? IncomeId { get; init; }
}
