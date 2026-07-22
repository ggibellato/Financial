namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for an expense record.
/// </summary>
public sealed class ExpenseDTO
{
    /// <summary>Expense identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Expense date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Free-text description.</summary>
    public required string Description { get; init; }

    /// <summary>Amount in GBP. Negative values represent a Reserva return or transfer out.</summary>
    public required decimal Value { get; init; }

    /// <summary>Expense category name.</summary>
    public required string Category { get; init; }

    /// <summary>Payment source name.</summary>
    public required string PaymentSource { get; init; }

    /// <summary>Optional credit-card tag name.</summary>
    public string? CardTag { get; init; }
}
