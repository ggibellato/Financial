namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to withdraw money from a single reserve bucket.
/// </summary>
public sealed class WithdrawalRequestDTO
{
    /// <summary>Bucket name to withdraw from.</summary>
    public required string Bucket { get; init; }

    /// <summary>Withdrawal amount, as a positive magnitude.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Withdrawal date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Reason for the withdrawal.</summary>
    public required string Description { get; init; }

    /// <summary>Set true to proceed despite an overdraft warning.</summary>
    public bool Confirmed { get; init; }
}
