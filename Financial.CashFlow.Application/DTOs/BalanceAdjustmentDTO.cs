namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a balance adjustment record.
/// </summary>
public sealed class BalanceAdjustmentDTO
{
    /// <summary>Balance adjustment identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Reconciliation date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Identifier of the bank this adjustment applies to.</summary>
    public required Guid BankId { get; init; }

    /// <summary>Name of the bank this adjustment applies to.</summary>
    public required string BankName { get; init; }

    /// <summary>The real balance entered, from the bank statement.</summary>
    public required decimal TargetBalance { get; init; }

    /// <summary>Computed correction: TargetBalance minus the balance as of Date, excluding this adjustment.</summary>
    public required decimal Delta { get; init; }

    /// <summary>Optional free-text note.</summary>
    public string? Note { get; init; }
}
