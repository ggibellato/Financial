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
    private readonly TimeProvider? _timeProviderOverride;
    private bool _disposed;

    public ApiTestFactory(IExchangeRateProvider? exchangeRateProviderOverride = null, TimeProvider? timeProviderOverride = null)
    {
        _dataFilePath = CreateTempDataFile();
        _cashFlowDataFilePath = CreateTempCashFlowDataFilePath();
        _exchangeRateProviderOverride = exchangeRateProviderOverride;
        _timeProviderOverride = timeProviderOverride;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Investment:Repository:Provider"] = "LocalJson",
                ["Investment:DataJsonFile"] = _dataFilePath,
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

        if (_timeProviderOverride is not null)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(_timeProviderOverride);
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

    // Mirrors the banks, income sources, and investment accounts a real deployment would have
    // after running the CashFlowSpreadsheetImport migration tool once (see BankMigrator /
    // IncomeSourceMigrator / InvestmentAccountMigrator).
    private const string SeededBanksJson = """
        {
          "Banks": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-100000000001", "Name": "Barclays", "RoundUpEnabled": false },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-100000000002", "Name": "Trading212", "RoundUpEnabled": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-100000000003", "Name": "Chase", "RoundUpEnabled": true }
          ],
          "IncomeSources": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-000000000001", "Name": "Gleison", "IsActive": true, "Group": "Salary" },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-000000000002", "Name": "Ariana", "IsActive": true, "Group": "Salary" },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-000000000003", "Name": "Lottery", "IsActive": true, "Group": "NonReportable" },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-000000000004", "Name": "DividendoJuros", "IsActive": true, "Group": "DividendoJuros" }
          ],
          "InvestmentAccounts": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000001", "Name": "BlueRewardsSaver", "IsActive": true, "IsLiability": false },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000002", "Name": "PlatinumVisa8003", "IsActive": true, "IsLiability": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000003", "Name": "PlatinumVisa6007", "IsActive": true, "IsLiability": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000004", "Name": "ChaseMaster4023", "IsActive": true, "IsLiability": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000005", "Name": "BaAmex", "IsActive": true, "IsLiability": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000006", "Name": "PaypalCredit", "IsActive": true, "IsLiability": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000007", "Name": "ChipCashIsaGleison", "IsActive": true, "IsLiability": false },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000008", "Name": "ChaseSave", "IsActive": true, "IsLiability": false },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000009", "Name": "ChipCashIsaAriana", "IsActive": true, "IsLiability": false },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000010", "Name": "Trading212Invested", "IsActive": true, "IsLiability": false },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-200000000011", "Name": "ReservasPessoais", "IsActive": true, "IsLiability": true }
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
