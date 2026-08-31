namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentAccountCreateDTO
{
    public required string Name { get; init; }

    public required bool IsActive { get; init; }

    public required bool IsLiability { get; init; }

    public required IReadOnlyList<string> Aliases { get; init; }
}
