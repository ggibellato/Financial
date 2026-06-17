using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Infrastructure.Integrations.FinancialToolSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

public sealed class GoogleGenerator : IGenerator
{
    private const int DelayBetweenSheetsMs = 3000;
    private const int DelayBetweenBrokersMs = 5000;
    private const int DelayBetweenOperationsMs = 1500;

    private readonly GoogleService _service;
    private readonly IJsonStorage _storage;
    private readonly IInvestmentsSerializer _serializer;
    private readonly GoogleSheetsAssetReader _sheetsReader;
    private readonly AssetMetadataResolver _metadataResolver;

    public GoogleGenerator(GoogleService service, IJsonStorage storage, GoogleGeneratorOptions options, IInvestmentsSerializer serializer)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _ = options ?? throw new ArgumentNullException(nameof(options));
        _sheetsReader = new GoogleSheetsAssetReader(service);
        _metadataResolver = new AssetMetadataResolver(options, _sheetsReader);
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

            if (brokerIndex > 1)
            {
                progress?.Report($"Waiting {DelayBetweenBrokersMs / 1000} seconds before next broker...");
                await Task.Delay(DelayBetweenBrokersMs);
            }

            var broker = Broker.Create(fileName, _metadataResolver.ResolveBrokerCurrency(fileName));
            data.AddBroker(broker);
            var file = files.FirstOrDefault(f => f.Name == fileName);

            progress?.Report($"Getting spreadsheets for: {fileName}");
            var spreadSheets = await _service.GetSpreadSheetAsync(file.Id);
            var activeSheets = spreadSheets.Where(s => !_metadataResolver.IsIgnoredSheet(s.Name)).ToList();

            int sheetCount = 0;
            foreach (var spreadsheet in activeSheets)
            {
                sheetCount++;
                progress?.Report($"[{fileName}] Processing sheet {sheetCount}/{activeSheets.Count}: {spreadsheet.Name}");

                var portfolioName = _metadataResolver.ResolvePortfolioName(fileName, spreadsheet);
                var portfolio = broker.AddPortfolio(portfolioName);

                var assetData = await _metadataResolver.ResolveAssetMetadataAsync(fileName, file!.Id, spreadsheet.Name);
                var asset = Asset.Create(
                    spreadsheet.Name,
                    assetData.isin,
                    assetData.exchangeId,
                    assetData.ticker,
                    assetData.country,
                    assetData.localTypeCode,
                    assetData.assetClass);
                portfolio.AddAsset(asset);

                asset.AddTransactions(await _sheetsReader.ReadTransactionsAsync(file.Id, spreadsheet.Name));

                await Task.Delay(DelayBetweenOperationsMs);

                asset.AddCredits(await _sheetsReader.ReadCreditsAsync(file.Id, spreadsheet.Name));

                if (sheetCount < activeSheets.Count)
                {
                    progress?.Report($"[{fileName}] Waiting {DelayBetweenSheetsMs / 1000} seconds before next sheet...");
                    await Task.Delay(DelayBetweenSheetsMs);
                }
            }
        }

        progress?.Report("Saving data...");
        var json = _serializer.Serialize(data);
        await _storage.WriteAsync(json);
        progress?.Report("Complete!");
    }
}
