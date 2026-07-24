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

    /// <summary>Income source name.</summary>
    public required string IncomeSource { get; init; }

    /// <summary>Gross value. Only meaningful for Gleison/Ariana entries.</summary>
    public decimal? GrossValue { get; init; }

    /// <summary>Net value received.</summary>
    public required decimal NetValue { get; init; }

    /// <summary>Destination bank name.</summary>
    public required string Bank { get; init; }
}
