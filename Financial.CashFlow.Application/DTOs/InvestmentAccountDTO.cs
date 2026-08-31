namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentAccountDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Whether this account should appear in an entry-form picklist.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this account is a liability (e.g. a credit card) rather than an asset.</summary>
    public required bool IsLiability { get; init; }

    public required IReadOnlyList<string> Aliases { get; init; }

    /// <summary>The account's most recent InvestmentSnapshot value (by Year, Month), or 0 when none
    /// exists. Delete is refused (409) while this is non-zero.</summary>
    public required decimal LatestBalance { get; init; }
}
