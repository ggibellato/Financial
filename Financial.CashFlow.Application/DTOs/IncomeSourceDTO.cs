namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a tracked income source.
/// </summary>
public sealed class IncomeSourceDTO
{
    /// <summary>Income source identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Income source name (resolution key used by Income.IncomeSource).</summary>
    public required string Name { get; init; }

    /// <summary>Whether this source should appear in an entry-form picklist.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Reporting group: "Salary", "DividendoJuros", or "NonReportable".</summary>
    public required string Group { get; init; }
}
