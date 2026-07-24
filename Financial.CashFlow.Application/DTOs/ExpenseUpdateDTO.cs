namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update an existing expense's details. The identifier comes from the route.
/// </summary>
public sealed class ExpenseUpdateDTO
{
    /// <summary>Expense date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Free-text description, up to 200 characters.</summary>
    public required string Description { get; init; }

    /// <summary>Amount in GBP. Negative values represent a Reserva return or transfer out.</summary>
    public required decimal Value { get; init; }

    /// <summary>Expense category name.</summary>
    public required string Category { get; init; }

    /// <summary>Payment source name. Omit when charging to a credit card.</summary>
    public string? PaymentSource { get; init; }

    /// <summary>Optional credit-card tag name.</summary>
    public string? CardTag { get; init; }
}
