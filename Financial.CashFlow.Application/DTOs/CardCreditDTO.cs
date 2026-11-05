namespace Financial.CashFlow.Application.DTOs;

public sealed class CardCreditDTO
{
    public required Guid AccountId { get; init; }
    public required string Name { get; init; }
    public required string Currency { get; init; }
    public required bool IsActive { get; init; }
    public required decimal Credit { get; init; }
}
