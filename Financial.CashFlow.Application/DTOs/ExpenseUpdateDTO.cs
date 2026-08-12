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

    /// <summary>Payment source bank identifier. Omit when charging to a credit card.</summary>
    public Guid? PaymentSourceBankId { get; init; }

    /// <summary>Optional credit card identifier. Omit when paying directly from a bank.</summary>
    public Guid? CreditCardId { get; init; }

    /// <summary>Invoice-period override for a credit card expense. Rejected if changed while the expense is already settled.</summary>
    public DateOnly? InvoiceDate { get; init; }

    /// <summary>Round-up amount. Full-replace: whatever is sent (including null) becomes the new stored value.</summary>
    public decimal? RoundUpAmount { get; init; }
}
