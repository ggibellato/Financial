namespace Financial.CashFlow.Application.DTOs;

public sealed class IncomeSourceCreateDTO
{
    public required string Name { get; init; }

    public required bool IsActive { get; init; }

    /// <summary>Reporting group: "Salary", "DividendoJuros", or "NonReportable".</summary>
    public required string Group { get; init; }

    public required bool AutoSplitToReserve { get; init; }
}
