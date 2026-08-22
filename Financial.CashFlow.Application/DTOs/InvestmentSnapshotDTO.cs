namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentSnapshotDTO
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    public required string AccountName { get; init; }
    public required bool IsLiability { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal Value { get; init; }
}
