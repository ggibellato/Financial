namespace Financial.CashFlow.Application.DTOs;

public sealed class TransferAccountOptionDTO
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Currency { get; init; }
}
