using Financial.Domain.Entities;
using Financial.Infrastructure.Integrations.FinancialToolSupport;
using Financial.Infrastructure.Integrations.GoogleFinancialSupport.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

public class GoogleGenerator : IGenerator
{
    // Rate limiting configuration to avoid API quota issues
    private const int DelayBetweenSheetsMs = 3000;  // 3 seconds between each sheet
    private const int DelayBetweenBrokersMs = 5000; // 5 seconds between each broker
    private const int DelayBetweenOperationsMs = 1500; // 1.5 seconds between reading operations and credits
    private const string DefaultPortfolioName = "Default";

    public List<string> IgnoreSpreadSheet = new List<string> {
        "Totais",
        "Totais com cotacao",
        "Recomendacoes",
        "Fundos de Investimento",
        "Opcoes"
    };

    public Dictionary<string, string> PortfolioName = new Dictionary<string, string>() {
        {"Trading 212_76a5af", "Fantastic Five Divid" },
        {"Trading 212_ffd966", "Almost Daily Dividen" },
        {"XPI_f4cccc", "Gold" },
        {"XPI_ffff", "Acoes" },
        {"XPI_cc0000", "Fundos Investimento" },
        {"XPI_222222", "Encerradas" },
        {"FreeTrade_222222", "Encerradas" },
        {"Trading 212_222222", "Encerradas" },
    };

    public Dictionary<string, string> ExchangeCurrency = new Dictionary<string, string>() {
        {"Trading 212", "GBP" },
        {"XPI", "BRL" },
        {"FreeTrade", "GBP" },
        {"Coinbase", "GBP" },
    };

    private readonly GoogleService _service;
    private readonly string _path;

    public GoogleGenerator(GoogleService service, string path) {
        _service = service;
        _path = path;
    }

    public async Task GenerateAsync(List<string> fileNames, IProgress<string> progress = null)
    {
        var data = Investments.Create();
        var files = await _service.GetFilesNameAsync();

        int brokerIndex = 0;
        foreach (var fileName in fileNames)
        {
            brokerIndex++;
            progress?.Report($"Processing broker {brokerIndex}/{fileNames.Count}: {fileName}");
            
            // Add delay between brokers to avoid rate limiting (except for first broker)
            if (brokerIndex > 1)
            {
                progress?.Report($"Waiting {DelayBetweenBrokersMs/1000} seconds before next broker...");
                await Task.Delay(DelayBetweenBrokersMs);
            }
            
            var exchange = Broker.Create(fileName, ExchangeCurrency[fileName]);
            data.AddBroker(exchange);
            var file = files.FirstOrDefault(f => f.Name == fileName);

            progress?.Report($"Getting spreadsheets for: {fileName}");
            var spreadSheets = await _service.GetSpreadSheetAsync(file.Id);
            
            int sheetCount = 0;
            int totalSheets = spreadSheets.Count(s => !IgnoreSpreadSheet.Contains(s.Name));
            
            foreach (var spreadsheet in spreadSheets)
            {
                if (IgnoreSpreadSheet.Contains(spreadsheet.Name))
                {
                    continue;
                }

                sheetCount++;
                progress?.Report($"[{fileName}] Processing sheet {sheetCount}/{totalSheets}: {spreadsheet.Name}");

                var portfolioName = ResolvePortfolioName(fileName, spreadsheet);
                var portfolio = exchange.AddPortfolio(portfolioName);

                var assetData = await ResolveAssetMetadataAsync(fileName, file!.Id, spreadsheet.Name);
                var asset = Asset.Create(spreadsheet.Name, assetData.isin, assetData.exchangeId, assetData.ticker);
                portfolio.AddAsset(asset);

                asset.AddOperations(await CreateOperationsAsync(file.Id, spreadsheet.Name));
                
                // Small delay between operations and credits
                await Task.Delay(DelayBetweenOperationsMs);

                asset.AddCredits(await CreateCreditsAsync(file.Id, spreadsheet.Name));
                
                // Delay between sheets to avoid rate limiting
                if (sheetCount < totalSheets)
                {
                    progress?.Report($"[{fileName}] Waiting {DelayBetweenSheetsMs/1000} seconds before next sheet...");
                    await Task.Delay(DelayBetweenSheetsMs);
                }
            }
        }
        progress?.Report("Saving data...");
        Save(data);
        progress?.Report("Complete!");
    }

    private async Task<(string isin, string exchangeId, string ticker)> GetAssetDataAsync(string id, string spreadSheetName)
    {
        var values = await _service.GetSpreadSheetDataAsync(id, $"{spreadSheetName}!Q2:S2");
        string isin = "";
        string exchangeId = "";
        string ticker = "";
        try
        {
            if(values is not null)
            {
                var data = values.FirstOrDefault();
                exchangeId = (string)data[0];
                ticker = (string)data[1];
                isin = (string)data[2];
            }
        }
        catch {
        }
        return (isin, exchangeId, ticker);
    }

    private string ResolvePortfolioName(string brokerName, SheetDTO spreadsheet)
    {
        var portfolioName = string.IsNullOrWhiteSpace(spreadsheet.Color) ? DefaultPortfolioName : spreadsheet.Color;
        if (PortfolioName.TryGetValue($"{brokerName}_{portfolioName}", out var name))
        {
            portfolioName = name;
        }

        return portfolioName;
    }

    private async Task<(string isin, string exchangeId, string ticker)> ResolveAssetMetadataAsync(
        string brokerName,
        string fileId,
        string spreadsheetName)
    {
        if (brokerName == "XPI")
        {
            return (string.Empty, "BVMF", spreadsheetName);
        }

        return await GetAssetDataAsync(fileId, spreadsheetName);
    }

    private void Save(Investments data)
    {
        string json = data.Serialize();
        File.WriteAllText(Path.Combine(_path, "data.json"), json);
    }

    private async Task<List<Operation>> CreateOperationsAsync(string id, string spreadSheetName)
    {
        var operations = new List<Operation>();
        // Use open-ended range to get all rows with data dynamically
        var values = await _service.GetSpreadSheetDataAsync(id, $"{spreadSheetName}!A3:G");
        var previousDate = 0L;

        foreach (var value in values)
        {
            var date = value[0] is long ? (long)value[0] : previousDate;
            previousDate = date;
            var type = (string)value[2];
                var quantity = GoogleSheetValueParser.ToDecimal(value[3]);
                var unitPrice = GoogleSheetValueParser.ToDecimal(value[5]);
                var fees = GoogleSheetValueParser.ToDecimal(value[6]) - (unitPrice * quantity);

            var operation = Operation.Create(
                    DateTime.FromOADate(date),
                    type == "V" ? Operation.OperationType.Sell : Operation.OperationType.Buy,
                    quantity,
                    unitPrice,
                    fees < 0 ? 0 : fees
                );
            operations.Add(operation);
        }
        return operations;
    }

    private async Task<List<Credit>> CreateCreditsAsync(string id, string spreadSheetName)
    {
        var credits = new List<Credit>();
        // Use open-ended range to get all rows with data dynamically
        var values = await _service.GetSpreadSheetDataAsync(id, $"{spreadSheetName}!K3:N");

        if (values == null)
        {
            return credits;
        }

        foreach (var value in values)
        {
            if(value.Count > 0 && !string.IsNullOrWhiteSpace(value[0].ToString()))
            {
                var type = value.Count > 3 ? (string)value[3] : "";
                var credit = Credit.Create(
                    DateTime.FromOADate((long)value[0]),
                    type == "Aluguel" ? Credit.CreditType.Rent : Credit.CreditType.Dividend,
                    GoogleSheetValueParser.ToDecimal(value[1])
                 );
                credits.Add(credit);
            }
        }
        return credits;
    }
}

