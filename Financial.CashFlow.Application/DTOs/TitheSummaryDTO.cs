namespace Financial.CashFlow.Application.DTOs;

public sealed class TitheSummaryDTO
{
    /// <summary>10% of the month's total income net value.</summary>
    public required decimal CalculatedTithe { get; init; }

    /// <summary>Calculated tithe minus the month's Dizimo-category expense total, plus the carried-in
    /// amount when included. May be negative.</summary>
    public required decimal TitheBalance { get; init; }

    /// <summary>The previous month's carry-forward, when a positive amount is available. Null when
    /// there is nothing to carry.</summary>
    public TitheCarryForwardDTO? CarryForward { get; init; }
}
