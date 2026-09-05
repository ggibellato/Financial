namespace Financial.CashFlow.Application.DTOs;

public sealed class CardStatementDTO
{
    public required Guid Id { get; init; }
    public required Guid CreditCardId { get; init; }
    public required string CreditCardName { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required bool IsPaid { get; init; }
    public required decimal OutstandingTotal { get; init; }

    /// <summary>Sum of every unpaid charge for this card across all invoice periods, not just
    /// this statement's own period.</summary>
    public required decimal AccumulatedOutstandingTotal { get; init; }

    /// <summary>Present only when a mark-paid call matched zero charges for this statement's invoice period.</summary>
    public string? Warning { get; init; }
}
