namespace Financial.CashFlow.Application.DTOs;

public sealed class TransferDTO
{
    public required Guid Id { get; init; }

    public required DateOnly Date { get; init; }

    public required Guid SourceBankId { get; init; }

    public required string SourceBankName { get; init; }

    public required Guid DestinationBankId { get; init; }

    public required string DestinationBankName { get; init; }

    public required decimal Amount { get; init; }

    public string? Note { get; init; }
}
