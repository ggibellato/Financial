namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a tracked investment account.
/// </summary>
public sealed class InvestmentAccountDTO
{
    /// <summary>Investment account identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Investment account name (resolution key used by InvestmentSnapshot.Account).</summary>
    public required string Name { get; init; }

    /// <summary>Whether this account should appear in an entry-form picklist.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this account is a liability (e.g. a credit card) rather than an asset.</summary>
    public required bool IsLiability { get; init; }
}
