namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentAccountDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Whether this account should appear in an entry-form picklist.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this account is a liability (e.g. a credit card) rather than an asset.</summary>
    public required bool IsLiability { get; init; }

    /// <summary>Whether any InvestmentSnapshot recorded for this account has a non-zero value.
    /// Delete is refused (409) while this is true.</summary>
    public required bool HasNonZeroInvestmentSnapshot { get; init; }

    /// <summary>Where a future suggested value for this account should come from: "None",
    /// "CreditCard", or "ReserveBucketsSum".</summary>
    public string Source { get; init; } = "None";

    /// <summary>The linked credit card, set only when <see cref="Source"/> is CreditCard.</summary>
    public Guid? CreditCardId { get; init; }
}
