namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountTransferCreateDTO
{
    public required DateOnly Date { get; init; }
    public required Guid SourceAccountId { get; init; }
    public required Guid DestinationAccountId { get; init; }
    public required decimal Amount { get; init; }
    public decimal? DestinationAmount { get; init; }
    public string? Note { get; init; }
}
