using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Financial.Api.Tests;

internal sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dataFilePath;
    private readonly string _cashFlowDataFilePath;
    private readonly IExchangeRateProvider? _exchangeRateProviderOverride;
    private bool _disposed;

    public ApiTestFactory(IExchangeRateProvider? exchangeRateProviderOverride = null)
    {
        _dataFilePath = CreateTempDataFile();
        _cashFlowDataFilePath = CreateTempCashFlowDataFilePath();
        _exchangeRateProviderOverride = exchangeRateProviderOverride;
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

        if (_exchangeRateProviderOverride is not null)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IExchangeRateProvider>();
                services.AddSingleton(_exchangeRateProviderOverride);
            });
        }
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

    private static string CreateTempCashFlowDataFilePath()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"financial-api-cashflow-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempPath, SeededBanksJson);
        return tempPath;
    }

    // Mirrors the banks and investment accounts a real deployment would have after running
    // the CashFlowSpreadsheetImport migration tool once (see BankMigrator / InvestmentAccountMigrator).
    private const string SeededBanksJson = """
        {
          "Banks": [
            { "Name": "Barclays", "RoundUpEnabled": false },
            { "Name": "Trading212", "RoundUpEnabled": true },
            { "Name": "Chase", "RoundUpEnabled": true }
          ],
          "InvestmentAccounts": [
            { "Name": "BlueRewardsSaver", "IsActive": true, "IsLiability": false },
            { "Name": "PlatinumVisa8003", "IsActive": true, "IsLiability": true },
            { "Name": "PlatinumVisa6007", "IsActive": true, "IsLiability": true },
            { "Name": "ChaseMaster4023", "IsActive": true, "IsLiability": true },
            { "Name": "BaAmex", "IsActive": true, "IsLiability": true },
            { "Name": "PaypalCredit", "IsActive": true, "IsLiability": true },
            { "Name": "ChipCashIsaGleison", "IsActive": true, "IsLiability": false },
            { "Name": "ChaseSave", "IsActive": true, "IsLiability": false },
            { "Name": "ChipCashIsaAriana", "IsActive": true, "IsLiability": false },
            { "Name": "Trading212Invested", "IsActive": true, "IsLiability": false },
            { "Name": "ReservasPessoais", "IsActive": true, "IsLiability": true }
          ]
        }
        """;

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
