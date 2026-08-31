namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentAccountUpdateDTO
{
    public required string Name { get; init; }

    public required bool IsActive { get; init; }

    public required bool IsLiability { get; init; }

    public required IReadOnlyList<string> Aliases { get; init; }
}
