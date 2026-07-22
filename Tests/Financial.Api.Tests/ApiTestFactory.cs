using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Financial.Api.Tests;

internal sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dataFilePath;
    private readonly string _cashFlowDataFilePath;
    private bool _disposed;

    public ApiTestFactory()
    {
        _dataFilePath = CreateTempDataFile();
        _cashFlowDataFilePath = CreateTempCashFlowDataFilePath();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Repository:Provider"] = "LocalJson",
                ["DataJsonFile"] = _dataFilePath,
                ["CashFlow:Repository:Provider"] = "LocalJson",
                ["CashFlow:DataJsonFile"] = _cashFlowDataFilePath
            };
            config.AddInMemoryCollection(settings);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && !_disposed)
        {
            _disposed = true;
            TryDeleteTempFile(_dataFilePath);
            TryDeleteTempFile(_cashFlowDataFilePath);
        }
    }

    private static string CreateTempDataFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"financial-api-{Guid.NewGuid():N}.json");
        File.Copy(TestDataPaths.DataJsonFile, tempPath, true);
        return tempPath;
    }

    private static string CreateTempCashFlowDataFilePath() =>
        Path.Combine(Path.GetTempPath(), $"financial-api-cashflow-{Guid.NewGuid():N}.json");

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Failed to delete temp data file '{path}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Failed to delete temp data file '{path}': {ex.Message}");
        }
    }
}
