namespace Financial.CashFlow.Application.DTOs;

public sealed class IncomeDTO
{
    public required Guid Id { get; init; }

    public required DateOnly Date { get; init; }

    public required Guid IncomeSourceId { get; init; }

    public required string IncomeSourceName { get; init; }

    /// <summary>Gross value. Only meaningful for Gleison/Ariana entries.</summary>
    public decimal? GrossValue { get; init; }

    public required decimal NetValue { get; init; }

    /// <summary>Destination bank identifier. Null when the income never lands in a tracked bank.</summary>
    public Guid? BankId { get; init; }

    /// <summary>Destination bank name. Null when the income never lands in a tracked bank.</summary>
    public string? BankName { get; init; }

    public string? Description { get; init; }
}
