namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update an existing transfer's details. The identifier comes from the route.
/// </summary>
public sealed class TransferUpdateDTO
{
    public required DateOnly Date { get; init; }

    public required Guid SourceBankId { get; init; }

    public required Guid DestinationBankId { get; init; }

    /// <summary>Amount moved in GBP. Must be greater than zero.</summary>
    public required decimal Amount { get; init; }

    public string? Note { get; init; }
}
