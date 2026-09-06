using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentAccountUpdateDTO
{
    public required string Name { get; init; }

    public required bool IsActive { get; init; }

    public required bool IsLiability { get; init; }

    public InvestmentAccountSource Source { get; init; } = InvestmentAccountSource.None;

    public Guid? CreditCardId { get; init; }
}
