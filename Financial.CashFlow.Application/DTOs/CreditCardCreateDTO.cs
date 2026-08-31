namespace Financial.CashFlow.Application.DTOs;

public sealed class CreditCardCreateDTO
{
    public required string Name { get; init; }

    public required bool IsActive { get; init; }
}
