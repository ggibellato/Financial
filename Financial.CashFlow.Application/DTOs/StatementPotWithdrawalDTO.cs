namespace Financial.CashFlow.Application.DTOs;

public sealed class StatementPotWithdrawalDTO
{
    public required Guid OperationId { get; init; }
    public required string PotsAccountName { get; init; }
    public required string PotName { get; init; }
    public required decimal Amount { get; init; }
}
