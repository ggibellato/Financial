namespace Financial.CashFlow.Application.DTOs;

public sealed class CurrencyTotalDTO
{
    public required string Currency { get; init; }
    public required decimal Balance { get; init; }
    public required decimal RoundUpTotal { get; init; }
    public required decimal CardOutstandingTotal { get; init; }
}
