using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;

const string ReservasSheetName = "Reservas";
const string MensaisSheetName = "Mensais";
const string ControleMaeSheetName = "Controle mae";
const string ResumoSheetPrefix = "Resumo";
const string MensaisOnlyFlag = "--mensais-only";

var mensaisOnly = args.Contains(MensaisOnlyFlag);
var positionalArgs = args.Where(a => a != MensaisOnlyFlag).ToArray();

var workbookPath = positionalArgs.Length > 0 ? positionalArgs[0] : @"C:\Users\ggibe\Downloads\Despesas.xlsx";
var outputPath = positionalArgs.Length > 1
    ? positionalArgs[1]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "data-cashflow.json"));

if (!File.Exists(workbookPath))
{
    Console.Error.WriteLine($"Workbook not found at '{workbookPath}'.");
    return 1;
}

var report = new ImportReport();
var serializer = new CashFlowSerializerAdapter();
var storage = new LocalJsonStorage(outputPath);
var data = mensaisOnly ? CashFlowLoader.LoadSync(storage, serializer) : CashFlowData.Create();

using var workbook = new XLWorkbook(workbookPath);

if (mensaisOnly)
{
    foreach (var bill in data.RecurringBills.ToList())
    {
        data.RemoveRecurringBill(bill.Id);
    }

    ImportMensaisSheet(workbook, data, report);
}
else
{
    ImportMonthlyExpenseSheets(workbook, data, report);
    ImportReservasSheet(workbook, data, report);
    ImportMensaisSheet(workbook, data, report);
    ImportControleMaeSheet(workbook, data, report);
    ImportResumoSheets(workbook, data, report);
}

var repository = new CashFlowJsonRepository(data, storage, serializer);
await repository.SaveChangesAsync();

Console.WriteLine($"Wrote imported data to '{outputPath}'.");
Console.WriteLine();
Console.WriteLine(report.Render());
return 0;

static void ImportMonthlyExpenseSheets(XLWorkbook workbook, CashFlowData data, ImportReport report)
{
    var monthlySheets = workbook.Worksheets
        .Select(sheet => (Sheet: sheet, Parsed: SheetNameParser.TryParseMonthlySheetName(sheet.Name, out var year, out var month), Year: year, Month: month))
        .Where(x => x.Parsed && SheetNameParser.IsInScope(x.Year, x.Month))
        .ToList();

    foreach (var (sheet, _, year, month) in monthlySheets)
    {
        var expenses = MonthlyExpenseSheetImporter.Import(sheet, year, month, report);
        foreach (var expense in expenses)
        {
            data.AddExpense(expense);
        }

        report.SheetImported(sheet.Name);
    }
}

static void ImportReservasSheet(XLWorkbook workbook, CashFlowData data, ImportReport report)
{
    if (!workbook.TryGetWorksheet(ReservasSheetName, out var sheet))
    {
        report.SheetSkipped(ReservasSheetName, "Sheet not found in workbook");
        return;
    }

    foreach (var movement in ReservasSheetImporter.Import(sheet))
    {
        data.AddReserveMovement(movement);
    }

    report.SheetImported(sheet.Name);
}

static void ImportMensaisSheet(XLWorkbook workbook, CashFlowData data, ImportReport report)
{
    if (!workbook.TryGetWorksheet(MensaisSheetName, out var sheet))
    {
        report.SheetSkipped(MensaisSheetName, "Sheet not found in workbook");
        return;
    }

    foreach (var bill in MensaisSheetImporter.Import(sheet))
    {
        data.AddRecurringBill(bill);
    }

    report.SheetImported(sheet.Name);
}

static void ImportControleMaeSheet(XLWorkbook workbook, CashFlowData data, ImportReport report)
{
    if (!workbook.TryGetWorksheet(ControleMaeSheetName, out var sheet))
    {
        report.SheetSkipped(ControleMaeSheetName, "Sheet not found in workbook");
        return;
    }

    foreach (var entry in ControleMaeSheetImporter.Import(sheet, report))
    {
        data.AddMaeLedgerEntry(entry);
    }

    report.SheetImported(sheet.Name);
}

static void ImportResumoSheets(XLWorkbook workbook, CashFlowData data, ImportReport report)
{
    var resumoSheets = workbook.Worksheets
        .Where(sheet => sheet.Name.StartsWith(ResumoSheetPrefix, StringComparison.OrdinalIgnoreCase))
        .Select(sheet => (Sheet: sheet, Parsed: int.TryParse(sheet.Name[ResumoSheetPrefix.Length..], out var year), Year: year))
        .Where(x => x.Parsed)
        .ToList();

    foreach (var (sheet, _, year) in resumoSheets)
    {
        if (year < SheetNameParser.FirstInScopeYear || year > SheetNameParser.LastInScopeYear)
        {
            report.SheetSkipped(
                sheet.Name,
                $"Year {year} predates the import's Feb {SheetNameParser.FirstInScopeYear}-{SheetNameParser.LastInScopeYear} scope");
            continue;
        }

        foreach (var snapshot in ResumoValidationReader.ImportAccountSnapshots(sheet, year))
        {
            data.AddInvestmentSnapshot(snapshot);
        }

        var sheetTotals = ResumoValidationReader.ReadYearlyExpenseTotals(sheet);
        if (sheetTotals is not null)
        {
            ValidateExpenseTotals(data, year, sheetTotals, report);
        }
        else
        {
            report.ValidationWarning($"{sheet.Name}: could not locate a 'Total despesas' row - skipping expense-total cross-check for this year");
        }

        report.SheetImported(sheet.Name);
    }
}

static void ValidateExpenseTotals(CashFlowData data, int year, IReadOnlyDictionary<int, decimal> sheetTotals, ImportReport report)
{
    const decimal tolerance = 0.01m;

    var computedTotals = data.Expenses
        .Where(e => e.Date.Year == year)
        .GroupBy(e => e.Date.Month)
        .ToDictionary(g => g.Key, g => g.Sum(e => e.Value));

    foreach (var (month, sheetTotal) in sheetTotals)
    {
        var computedTotal = computedTotals.GetValueOrDefault(month, 0m);
        if (Math.Abs(computedTotal - sheetTotal) > tolerance)
        {
            report.ValidationWarning(
                $"Resumo{year} {month:D2}: sheet total {sheetTotal:F2} vs imported total {computedTotal:F2} (diff {computedTotal - sheetTotal:F2})");
        }
    }
}
