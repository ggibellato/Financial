using Financial.Integrations.Frankfurter;
using Financial.Investment.Application.Services;
using Financial.Investment.CurrencyBackfill;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

var dataFilePath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "data-investment.json"));

if (!File.Exists(dataFilePath))
{
    Console.Error.WriteLine($"Data file not found at '{dataFilePath}'.");
    return 1;
}

var backupPath = CreateBackup(dataFilePath);
Console.WriteLine($"Backed up data file to '{backupPath}'.");

var serializer = new InvestmentSerializerAdapter();
var storage = new LocalJsonStorage(dataFilePath);
var investments = InvestmentLoader.LoadSync(storage, serializer);

using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.frankfurter.app/") };
var exchangeRateProvider = new FrankfurterExchangeRateProvider(httpClient, NullLogger<FrankfurterExchangeRateProvider>.Instance);
var reportingCurrencyProvider = new FixedReportingCurrencyProvider();

var summary = await CurrencyBackfillMigrator.MigrateAsync(investments, exchangeRateProvider, reportingCurrencyProvider.GetReportingCurrency());

var repository = new InvestmentJsonRepository(investments, storage, serializer);
await repository.ApplyAndSaveAsync(() => true);

Console.WriteLine($"Wrote backfilled data to '{dataFilePath}'.");
Console.WriteLine();
Console.WriteLine(summary.Render());

return 0;

static string CreateBackup(string dataPath)
{
    var directory = Path.GetDirectoryName(Path.GetFullPath(dataPath))!;
    var name = Path.GetFileNameWithoutExtension(dataPath);
    var extension = Path.GetExtension(dataPath);
    var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    var backupPath = Path.Combine(directory, $"{name}.backup-currency-backfill-{timestamp}{extension}");

    var suffix = 1;
    while (File.Exists(backupPath))
    {
        backupPath = Path.Combine(directory, $"{name}.backup-currency-backfill-{timestamp}-{suffix}{extension}");
        suffix++;
    }

    File.Copy(dataPath, backupPath);
    return backupPath;
}
