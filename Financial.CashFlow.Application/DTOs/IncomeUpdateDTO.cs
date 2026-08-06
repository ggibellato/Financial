namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update an existing income entry's details. The identifier comes from the route.
/// </summary>
public sealed class IncomeUpdateDTO
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
