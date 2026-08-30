namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update a credit card. Full replace of every mutable field, including <c>Name</c>.
/// </summary>
public sealed class CreditCardUpdateDTO
{
    public required string Name { get; init; }

    public required bool IsActive { get; init; }

    /// <summary>New due date, or <c>null</c> to clear it.</summary>
    public DateOnly? NextInvoiceDueDate { get; init; }
}
