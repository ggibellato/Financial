namespace Financial.CashFlow.Application.DTOs;

public sealed class FundingLineDTO
{
    public required Guid BankId { get; init; }
    public required decimal Amount { get; init; }
}
