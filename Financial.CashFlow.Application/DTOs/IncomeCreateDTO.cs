namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to create a new income entry. The server generates the identifier.
/// </summary>
public sealed class IncomeCreateDTO
{
    public required DateOnly Date { get; init; }

    public required Guid IncomeSourceId { get; init; }

    /// <summary>Gross value. Only meaningful for Gleison/Ariana entries.</summary>
    public decimal? GrossValue { get; init; }

    public required decimal NetValue { get; init; }

    /// <summary>Destination bank identifier. Omit when the income never lands in a tracked bank.</summary>
    public Guid? BankId { get; init; }

    /// <summary>Free-text description, up to 200 characters. Optional.</summary>
    public string? Description { get; init; }

    public bool SplitToReserve { get; init; } = false;
}
