namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Incomes.SpreadsheetImport;

public readonly record struct IncomeTotal(string Source, decimal? GrossValue, decimal NetValue);
