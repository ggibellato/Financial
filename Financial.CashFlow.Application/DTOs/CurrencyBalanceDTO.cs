namespace Financial.CashFlow.Application.DTOs;

public sealed class CurrencyBalanceDTO
{
    public required string Currency { get; init; }
    public required decimal Balance { get; init; }
}
