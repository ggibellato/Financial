namespace Financial.CashFlow.Application.DTOs;

public sealed class CreditCardDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Whether this card currently accepts new expenses.</summary>
    public required bool IsActive { get; init; }

    public DateOnly? NextInvoiceDueDate { get; init; }
}
