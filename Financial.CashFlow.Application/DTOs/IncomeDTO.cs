namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for an income record.
/// </summary>
public sealed class IncomeDTO
{
    /// <summary>Income identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Income date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Income source identifier.</summary>
    public required Guid IncomeSourceId { get; init; }

    /// <summary>Income source name.</summary>
    public required string IncomeSourceName { get; init; }

    /// <summary>Gross value. Only meaningful for Gleison/Ariana entries.</summary>
    public decimal? GrossValue { get; init; }

    /// <summary>Net value received.</summary>
    public required decimal NetValue { get; init; }

    /// <summary>Destination bank identifier. Null when the income never lands in a tracked bank.</summary>
    public Guid? BankId { get; init; }

    /// <summary>Destination bank name. Null when the income never lands in a tracked bank.</summary>
    public string? BankName { get; init; }

    /// <summary>Free-text description. Null when omitted.</summary>
    public string? Description { get; init; }
}
