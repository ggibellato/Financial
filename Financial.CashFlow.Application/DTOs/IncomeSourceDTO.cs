namespace Financial.CashFlow.Application.DTOs;

public sealed class IncomeSourceDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Whether this source should appear in an entry-form picklist.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Reporting group: "Salary", "DividendoJuros", or "NonReportable".</summary>
    public required string Group { get; init; }

    /// <summary>Whether the Income form should offer a "split to reserve" option for this source.</summary>
    public required bool AutoSplitToReserve { get; init; }
}
