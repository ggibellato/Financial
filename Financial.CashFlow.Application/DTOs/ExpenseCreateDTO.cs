namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to create a new expense. The server generates the identifier.
/// </summary>
public sealed class ExpenseCreateDTO
{
    /// <summary>Expense date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Free-text description, up to 200 characters.</summary>
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
