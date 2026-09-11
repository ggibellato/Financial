using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.DataQualityReport;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Observability;
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

var serializer = new InvestmentSerializerAdapter();
var storage = new LocalJsonStorage(dataFilePath);
var investments = InvestmentLoader.LoadSync(storage, serializer);
var repository = new InvestmentJsonRepository(investments, storage, serializer);

IDataQualityReportService service = new DataQualityReportService(
    repository, NoOpTelemetryTracer.Instance, NullLogger<DataQualityReportService>.Instance);

var report = service.GenerateReport();

Console.WriteLine($"Data-quality report for '{dataFilePath}'");
Console.WriteLine(new string('=', 60));
Console.WriteLine();
Console.WriteLine(DataQualityReportFormatter.Format(report));

return 0;
