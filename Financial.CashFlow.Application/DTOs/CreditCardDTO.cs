namespace Financial.CashFlow.Application.DTOs;

public sealed class CreditCardDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Whether this card currently accepts new expenses.</summary>
    public required bool IsActive { get; init; }

    public DateOnly? NextInvoiceDueDate { get; init; }

    /// <summary>The latest (most future) invoice month among this card's expenses, if any -
    /// used to smart-default a new charge's invoice month ahead of the plain date-derived one.</summary>
    public DateOnly? LatestInvoiceDate { get; init; }

    /// <summary>Whether an expense or card statement still references this card - Delete is
    /// refused (409) while this is true.</summary>
    public required bool HasReferences { get; init; }
}
