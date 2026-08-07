namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a transfer record.
/// </summary>
public sealed class TransferDTO
{
    /// <summary>Transfer identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Transfer date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Identifier of the bank the money leaves.</summary>
    public required Guid SourceBankId { get; init; }

    /// <summary>Name of the bank the money leaves.</summary>
    public required string SourceBankName { get; init; }

    /// <summary>Identifier of the bank the money enters.</summary>
    public required Guid DestinationBankId { get; init; }

    /// <summary>Name of the bank the money enters.</summary>
    public required string DestinationBankName { get; init; }

    /// <summary>Amount moved in GBP.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Optional free-text note.</summary>
    public string? Note { get; init; }
}
