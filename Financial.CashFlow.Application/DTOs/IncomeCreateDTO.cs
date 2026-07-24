namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to create a new income entry. The server generates the identifier.
/// </summary>
public sealed class IncomeCreateDTO
{
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
