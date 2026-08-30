namespace Financial.CashFlow.Application.DTOs;

public sealed class BankDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Whether this bank rounds up card payments.</summary>
    public required bool RoundUpEnabled { get; init; }

    /// <summary>Real-world balance as of <see cref="OpeningBalanceDate"/>.</summary>
    public required decimal OpeningBalance { get; init; }

    /// <summary>The date <see cref="OpeningBalance"/> is accurate as of.</summary>
    public required DateOnly OpeningBalanceDate { get; init; }

    /// <summary>Whether a balance adjustment, income, expense, or transfer still references this
    /// bank - Delete is refused (409) while this is true.</summary>
    public required bool HasReferences { get; init; }
}
