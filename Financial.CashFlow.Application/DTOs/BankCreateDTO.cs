namespace Financial.CashFlow.Application.DTOs;

public sealed class BankCreateDTO
{
    public required string Name { get; init; }

    public required bool RoundUpEnabled { get; init; }
}
