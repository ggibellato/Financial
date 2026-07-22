using ClosedXML.Excel;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;

var workbookPath = args.Length > 0 ? args[0] : @"C:\Users\ggibe\Downloads\Despesas.xlsx";
var report = new ImportReport();

if (!File.Exists(workbookPath))
{
    Console.Error.WriteLine($"Workbook not found at '{workbookPath}'.");
    return 1;
}

using var workbook = new XLWorkbook(workbookPath);

var monthlySheets = workbook.Worksheets
    .Select(sheet => (Sheet: sheet, Parsed: SheetNameParser.TryParseMonthlySheetName(sheet.Name, out var year, out var month), Year: year, Month: month))
    .Where(x => x.Parsed && SheetNameParser.IsInScope(x.Year, x.Month))
    .ToList();

Console.WriteLine($"Found {monthlySheets.Count} in-scope monthly tabs.");

foreach (var (sheet, _, year, month) in monthlySheets)
{
    var expenses = MonthlyExpenseSheetImporter.Import(sheet, year, month, report);
    report.SheetImported(sheet.Name);
    Console.WriteLine($"  {sheet.Name}: {expenses.Count} expenses");
}

Console.WriteLine();
Console.WriteLine(report.Render());
return 0;
