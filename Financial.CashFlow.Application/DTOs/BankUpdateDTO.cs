namespace Financial.CashFlow.Application.DTOs;

public sealed class BankUpdateDTO
{
    public required string Name { get; init; }

    public required bool RoundUpEnabled { get; init; }
}
