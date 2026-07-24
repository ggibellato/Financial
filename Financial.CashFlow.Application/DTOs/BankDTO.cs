namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a tracked bank.
/// </summary>
public sealed class BankDTO
{
    /// <summary>Bank name.</summary>
    public required string Name { get; init; }

    /// <summary>Whether this bank rounds up card payments.</summary>
    public required bool RoundUpEnabled { get; init; }
}
