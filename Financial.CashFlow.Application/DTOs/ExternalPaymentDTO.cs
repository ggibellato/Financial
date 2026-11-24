namespace Financial.CashFlow.Application.DTOs;

public sealed class ExternalPaymentDTO
{
    public required Guid Id { get; init; }
    public required string Direction { get; init; }
    public required DateOnly Date { get; init; }
    public required Guid AccountId { get; init; }
    public required string AccountName { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
}
