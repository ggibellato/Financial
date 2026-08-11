namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a tracked credit card.
/// </summary>
public sealed class CreditCardDTO
{
    /// <summary>Credit card identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Credit card name (resolution key used by Expense.CreditCard/CardStatement.CreditCard).</summary>
    public required string Name { get; init; }

    /// <summary>Whether this card currently accepts new expenses.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Next invoice due date, if set.</summary>
    public DateOnly? NextInvoiceDueDate { get; init; }
}
