namespace Financial.CashFlow.Application.DTOs;

public sealed class TitheSummaryDTO
{
    /// <summary>10% of the month's total income net value.</summary>
    public required decimal CalculatedTithe { get; init; }

    /// <summary>Calculated tithe minus the month's Dizimo-category expense total. May be negative.</summary>
    public required decimal TitheBalance { get; init; }
}
