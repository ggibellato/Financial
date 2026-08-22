namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentAccountDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Whether this account should appear in an entry-form picklist.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this account is a liability (e.g. a credit card) rather than an asset.</summary>
    public required bool IsLiability { get; init; }
}
