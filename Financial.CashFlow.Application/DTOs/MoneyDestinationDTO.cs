namespace Financial.CashFlow.Application.DTOs;

public sealed class MoneyDestinationDTO
{
    public required string Kind { get; init; }
    public Guid? AccountId { get; init; }
    public string? AccountName { get; init; }
}
