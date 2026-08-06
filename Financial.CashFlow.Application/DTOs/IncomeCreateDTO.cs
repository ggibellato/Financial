namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to create a new income entry. The server generates the identifier.
/// </summary>
public sealed class IncomeCreateDTO
{
    /// <summary>Income date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Income source identifier.</summary>
    public required Guid IncomeSourceId { get; init; }

    /// <summary>Gross value. Only meaningful for Gleison/Ariana entries.</summary>
    public decimal? GrossValue { get; init; }

    /// <summary>Net value received.</summary>
    public required decimal NetValue { get; init; }

    /// <summary>Destination bank identifier.</summary>
    public required Guid BankId { get; init; }
}
