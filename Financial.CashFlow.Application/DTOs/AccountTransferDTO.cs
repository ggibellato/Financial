namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountTransferDTO
{
    public required Guid Id { get; init; }
    public required DateOnly Date { get; init; }
    public required Guid SourceAccountId { get; init; }
    public required string SourceAccountName { get; init; }
    public required string SourceCurrency { get; init; }
    public required Guid DestinationAccountId { get; init; }
    public required string DestinationAccountName { get; init; }
    public required string DestinationCurrency { get; init; }
    public required decimal Amount { get; init; }
    public required decimal DestinationAmount { get; init; }
    public string? Note { get; init; }
}
