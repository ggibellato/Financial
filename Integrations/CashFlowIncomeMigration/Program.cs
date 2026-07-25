using ClosedXML.Excel;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowIncomeMigration;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;

var dataPath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "data-cashflow.json"));
var workbookPath = args.Length > 1 ? args[1] : @"C:\Users\ggibe\Downloads\Despesas.xlsx";

if (!File.Exists(dataPath))
{
    Console.Error.WriteLine($"Data file not found at '{dataPath}'.");
    return 1;
}

if (!File.Exists(workbookPath))
{
    Console.Error.WriteLine($"Workbook not found at '{workbookPath}'.");
    return 1;
}

var backupPath = MigrationBackup.Create(dataPath);
Console.WriteLine($"Backed up data file to '{backupPath}'.");

var serializer = new CashFlowSerializerAdapter();
var storage = new LocalJsonStorage(dataPath);
var data = CashFlowLoader.LoadSync(storage, serializer);

using var workbook = new XLWorkbook(workbookPath);
var summary = IncomeMigrator.Migrate(data, workbook);

var repository = new CashFlowJsonRepository(data, storage, serializer);
await repository.SaveChangesAsync();

Console.WriteLine($"Wrote migrated data to '{dataPath}'.");
Console.WriteLine();
Console.WriteLine(summary.Render());
return 0;
