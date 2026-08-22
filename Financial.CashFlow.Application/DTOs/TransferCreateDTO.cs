namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to create a new transfer. The server generates the identifier.
/// </summary>
public sealed class TransferCreateDTO
{
    public required DateOnly Date { get; init; }

    public required Guid SourceBankId { get; init; }

    public required Guid DestinationBankId { get; init; }

    /// <summary>Amount moved in GBP. Must be greater than zero.</summary>
    public required decimal Amount { get; init; }

    public string? Note { get; init; }
}
