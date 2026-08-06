namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a tracked bank.
/// </summary>
public sealed class BankDTO
{
    /// <summary>Bank identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Bank name.</summary>
    public required string Name { get; init; }

    /// <summary>Whether this bank rounds up card payments.</summary>
    public required bool RoundUpEnabled { get; init; }

    /// <summary>Real-world balance as of <see cref="OpeningBalanceDate"/>.</summary>
    public required decimal OpeningBalance { get; init; }

    /// <summary>The date <see cref="OpeningBalance"/> is accurate as of.</summary>
    public required DateOnly OpeningBalanceDate { get; init; }
}
