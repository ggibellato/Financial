namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update a credit card's next invoice due date and active flag. Full replace of
/// both mutable fields; <c>Name</c> is immutable via this endpoint.
/// </summary>
public sealed class CreditCardUpdateDTO
{
    /// <summary>New due date, or <c>null</c> to clear it.</summary>
    public DateOnly? NextInvoiceDueDate { get; init; }

    public required bool IsActive { get; init; }
}
