namespace Financial.CashFlow.Application.DTOs;

public sealed class BankDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required bool RoundUpEnabled { get; init; }

    /// <summary>Real-world balance as of <see cref="OpeningBalanceDate"/>.</summary>
    public required decimal OpeningBalance { get; init; }

    public required DateOnly OpeningBalanceDate { get; init; }

    /// <summary>Whether a balance adjustment, income, expense, or transfer still references this
    /// bank - Delete is refused (409) while this is true.</summary>
    public required bool HasReferences { get; init; }
}
