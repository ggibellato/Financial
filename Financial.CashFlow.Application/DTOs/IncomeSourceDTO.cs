namespace Financial.CashFlow.Application.DTOs;

public sealed class IncomeSourceDTO
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required bool IsActive { get; init; }

    /// <summary>Reporting group: "Salary", "DividendoJuros", or "NonReportable".</summary>
    public required string Group { get; init; }

    public required bool AutoSplitToReserve { get; init; }

    /// <summary>Whether an Income entry still references this source. Delete is refused (409) while this is true.</summary>
    public required bool HasReferences { get; init; }
}
