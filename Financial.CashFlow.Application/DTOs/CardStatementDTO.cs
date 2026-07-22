namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a card statement, joined with its computed outstanding total.
/// </summary>
public sealed class CardStatementDTO
{
    public required Guid Id { get; init; }
    public required string Card { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required bool IsPaid { get; init; }
    public required decimal OutstandingTotal { get; init; }
}
