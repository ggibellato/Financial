namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Incomes.SpreadsheetImport;

/// <summary>
/// One income source's total, as read from a single monthly sheet's totals area.
/// </summary>
public readonly record struct IncomeTotal(string Source, decimal? GrossValue, decimal NetValue);
