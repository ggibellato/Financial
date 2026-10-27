namespace Financial.CashFlow.Application.DTOs;

public sealed class PayableStatementDTO
{
    public required Guid Id { get; init; }
    public required Guid CreditCardId { get; init; }
    public required string CreditCardName { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal Total { get; init; }
}
