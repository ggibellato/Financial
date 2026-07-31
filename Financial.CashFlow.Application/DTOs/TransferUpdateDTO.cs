namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update an existing transfer's details. The identifier comes from the route.
/// </summary>
public sealed class TransferUpdateDTO
{
    /// <summary>Transfer date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Bank the money leaves.</summary>
    public required string SourceBank { get; init; }

    /// <summary>Bank the money enters.</summary>
    public required string DestinationBank { get; init; }

    /// <summary>Amount moved in GBP. Must be greater than zero.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Optional free-text note.</summary>
    public string? Note { get; init; }
}
