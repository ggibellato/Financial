using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Banks;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.BankOpeningBalance;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCards;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Categories;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Incomes;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.IncomeSources;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBuckets;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBucketReferences;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCardReferences;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CategoryReferences;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.EntityReferences;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ExpenseChargeDate;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Parsing;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.SheetImporters;
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

string? legacyRawJson = null;
EntityReferenceMigrationSummary? entityReferenceSummary = null;
ReserveBucketReferenceMigrationSummary? reserveBucketReferenceSummary = null;
CreditCardReferenceMigrationSummary? creditCardReferenceSummary = null;
CategoryReferenceMigrationSummary? categoryReferenceSummary = null;
if (File.Exists(outputPath))
{
    var backupPath = MigrationBackup.Create(outputPath);
    Console.WriteLine($"Backed up data file to '{backupPath}'.");
    // Captured before any typed load happens: this raw text is the only place a pre-existing
    // settled expense's SettledAt value can still be read from, since Expense no longer
    // declares that property and a normal deserialization silently drops it.
    legacyRawJson = File.ReadAllText(backupPath);

    // All four must run before CashFlowLoader.LoadSync below: the typed deserializer throws on
    // a file still carrying any legacy shape, so every rewrite has to happen on the raw file
    // first. Each runs in the order its own reference transition landed, since a file could in
    // principle need all four rewrites at once (an old file that predates every migration).
    entityReferenceSummary = EntityReferenceMigrator.Migrate(outputPath);
    reserveBucketReferenceSummary = ReserveBucketReferenceMigrator.Migrate(outputPath);
    creditCardReferenceSummary = CreditCardReferenceMigrator.Migrate(outputPath);
    categoryReferenceSummary = CategoryReferenceMigrator.Migrate(outputPath);
}

var report = new ImportReport();
var serializer = new CashFlowSerializerAdapter();
var storage = new LocalJsonStorage(outputPath);
var today = DateOnly.FromDateTime(DateTime.Now);

// Loaded once up front regardless of mode: this is also where Banks/CardStatements come from
// for a full rebuild below, since the spreadsheet never produces those itself (Incomes are
// carried over here too, but also get backfilled from the spreadsheet separately below via
// IncomeMigrator/IncomeBackfillImporter).
var existingData = CashFlowLoader.LoadSync(storage, serializer);
var data = mensaisOnly ? existingData : CashFlowData.Create();

// Carrying over data the spreadsheet doesn't own, and seeding/backfilling the investment
// account registry, must both happen before ImportResumoSheets: dynamic account resolution
// there depends on the registry (existing accounts + this run's seed table) already being
// in place. Safe to run unconditionally in mensaisOnly mode too, since data == existingData
// there and both operations are no-ops/idempotent against already-present data.
// The migrator also audits existing snapshots against the registry, but no snapshots exist
// in `data` yet at this point in a full run - that audit result is discarded here and
// recomputed for real once ImportResumoSheets has populated InvestmentSnapshots below.
if (!mensaisOnly)
{
    CarryOverDataTheSpreadsheetDoesNotOwn(existingData, data);
}

InvestmentAccountMigrator.Migrate(data);

// Must also run before the expense sheets are imported below: MonthlyExpenseSheetImporter
// resolves each row's payment source against data.Banks (F03), so the 3 tracked banks need to
// already exist even on a from-scratch run where no bank was carried over from an existing
// file. Seeding is idempotent, so re-running it at the end (below) to audit the final
// imported expense set is safe.
BankMigrator.Migrate(data);

// Must also run before ImportReservasSheet below: once ReserveMovement references a real
// ReserveBucket (F02), the importer resolves each column's bucket by name against
// data.ReserveBuckets, so the 4 tracked buckets need to already exist. Seeding is idempotent,
// so re-running it at the end (below) to audit the final imported movement set is safe.
ReserveBucketMigrator.Migrate(data);

// Must also run before the expense sheets are imported below, same reason as BankMigrator
// above: MonthlyExpenseSheetImporter resolves each row's card-mode charges against
// data.CreditCards (P29) and every row's category against data.Categories (P30) - on a
// from-scratch run neither collection has been seeded yet at this point. Without this, every
// row fails category resolution and the whole import silently comes back empty. Seeding is
// idempotent, so re-running both at the end (below) to audit the final imported expense set
// is safe.
CreditCardMigrator.Migrate(data);
CategoryMigrator.Migrate(data);

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
    ImportMonthlyExpenseSheets(workbook, data, report, today);
    ImportReservasSheet(workbook, data, report);
    ImportMensaisSheet(workbook, data, report);
    ImportControleMaeSheet(workbook, data, report);
    ImportResumoSheets(workbook, data, report);
}

// Always run, in both modes: every migration below is idempotent, so re-running is always safe.
var bankSummary = BankMigrator.Migrate(data);
var bankOpeningBalanceSummary = BankOpeningBalanceMigrator.Migrate(data, today);
var incomeSummary = IncomeMigrator.Migrate(data, workbook);
// Runs after IncomeMigrator so its audit of Income.IncomeSource values covers backfilled
// entries too, not just what was already on the data file before this run.
var incomeSourceSummary = IncomeSourceMigrator.Migrate(data);
var creditCardSummary = CreditCardMigrator.Migrate(data);
var categorySummary = CategoryMigrator.Migrate(data);
// Re-run (seeding is idempotent) so the reported summary's movement audit and split-percentage
// warning reflect the reserve movements ImportReservasSheet just added above.
var reserveBucketSummary = ReserveBucketMigrator.Migrate(data);
var expenseChargeDateSummary = ExpenseChargeDateMigrator.Migrate(data, legacyRawJson);
// Re-run (seeding is idempotent) so the reported summary's snapshot audit reflects the
// snapshots ImportResumoSheets just added above, not the empty pre-import state.
var investmentAccountSummary = InvestmentAccountMigrator.Migrate(data);

var repository = new CashFlowJsonRepository(data, storage, serializer);
await repository.SaveChangesAsync();

Console.WriteLine($"Wrote imported data to '{outputPath}'.");
Console.WriteLine();
if (entityReferenceSummary is not null)
{
    Console.WriteLine(entityReferenceSummary.Render());
}
if (reserveBucketReferenceSummary is not null)
{
    Console.WriteLine(reserveBucketReferenceSummary.Render());
}
if (creditCardReferenceSummary is not null)
{
    Console.WriteLine(creditCardReferenceSummary.Render());
}
if (categoryReferenceSummary is not null)
{
    Console.WriteLine(categoryReferenceSummary.Render());
}
Console.WriteLine(report.Render());
Console.WriteLine(bankSummary.Render());
Console.WriteLine(bankOpeningBalanceSummary.Render());
Console.WriteLine(incomeSummary.Render());
Console.WriteLine(incomeSourceSummary.Render());
Console.WriteLine(creditCardSummary.Render());
Console.WriteLine(categorySummary.Render());
Console.WriteLine(reserveBucketSummary.Render());
Console.WriteLine(expenseChargeDateSummary.Render());
Console.WriteLine(investmentAccountSummary.Render());
return 0;

static void CarryOverDataTheSpreadsheetDoesNotOwn(CashFlowData existingData, CashFlowData data)
{
    foreach (var bank in existingData.Banks)
    {
        data.AddBank(bank);
    }

    foreach (var incomeSource in existingData.IncomeSources)
    {
        data.AddIncomeSource(incomeSource);
    }

    foreach (var creditCard in existingData.CreditCards)
    {
        data.AddCreditCard(creditCard);
    }

    foreach (var reserveBucket in existingData.ReserveBuckets)
    {
        data.AddReserveBucket(reserveBucket);
    }

    foreach (var income in existingData.Incomes)
    {
        data.AddIncome(income);
    }

    foreach (var statement in existingData.CardStatements)
    {
        data.AddCardStatement(statement);
    }

    foreach (var account in existingData.InvestmentAccounts)
    {
        data.AddInvestmentAccount(account);
    }
}

static void ImportMonthlyExpenseSheets(XLWorkbook workbook, CashFlowData data, ImportReport report, DateOnly today)
{
    var monthlySheets = workbook.Worksheets
        .Select(sheet => (Sheet: sheet, Parsed: SheetNameParser.TryParseMonthlySheetName(sheet.Name, out var year, out var month), Year: year, Month: month))
        .Where(x => x.Parsed && SheetNameParser.IsInScope(x.Year, x.Month))
        .ToList();

    foreach (var (sheet, _, year, month) in monthlySheets)
    {
        var expenses = MonthlyExpenseSheetImporter.Import(sheet, year, month, today, report, data.Banks, data.CreditCards, data.Categories);
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

    foreach (var movement in ReservasSheetImporter.Import(sheet, data.ReserveBuckets, report))
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

        foreach (var snapshot in ResumoValidationReader.ImportAccountSnapshots(sheet, year, data.InvestmentAccounts, report))
        {
            data.AddInvestmentSnapshot(snapshot);
        }

        var sheetTotals = ResumoValidationReader.ReadAnnualExpenseTotals(sheet);
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
