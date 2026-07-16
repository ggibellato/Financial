using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

public sealed class GoogleGenerator
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

        for (var brokerIndex = 0; brokerIndex < fileNames.Count; brokerIndex++)
        {
            var fileName = fileNames[brokerIndex];
            progress?.Report($"Processing broker {brokerIndex + 1}/{fileNames.Count}: {fileName}");

            if (brokerIndex > 0)
            {
                progress?.Report($"Waiting {DelayBetweenBrokersMs / 1000} seconds before next broker...");
                await Task.Delay(DelayBetweenBrokersMs);
            }

            var file = files.FirstOrDefault(f => f.Name == fileName);
            await ProcessBrokerAsync(data, fileName, file?.Id, progress);
        }

        progress?.Report("Saving data...");
        var json = _serializer.Serialize(data);
        await _storage.WriteAsync(json);
        progress?.Report("Complete!");
    }

    private async Task ProcessBrokerAsync(Investments data, string fileName, string fileId, IProgress<string> progress)
    {
        var currency = _metadataResolver.ResolveBrokerCurrency(fileName);
        Broker activeBroker = null;
        Broker historicBroker = null;

        progress?.Report($"Getting spreadsheets for: {fileName}");
        var spreadSheets = await _service.GetSpreadSheetAsync(fileId);
        var activeSheets = spreadSheets.Where(s => !_metadataResolver.IsIgnoredSheet(s.Name)).ToList();

        for (var sheetIndex = 0; sheetIndex < activeSheets.Count; sheetIndex++)
        {
            var spreadsheet = activeSheets[sheetIndex];
            progress?.Report($"[{fileName}] Processing sheet {sheetIndex + 1}/{activeSheets.Count}: {spreadsheet.Name}");

            var resolvedPortfolioName = _metadataResolver.ResolvePortfolioName(fileName, spreadsheet);

            if (_metadataResolver.IsClosedPortfolio(resolvedPortfolioName))
            {
                historicBroker ??= CreateBroker(data, fileName, currency, isHistoric: true);
                var historicPortfolioName = _metadataResolver.ResolveHistoricPortfolioName(spreadsheet.Name);
                await ProcessSheetAsync(historicBroker, historicPortfolioName, fileName, fileId, spreadsheet, progress);
            }
            else
            {
                activeBroker ??= CreateBroker(data, fileName, currency, isHistoric: false);
                await ProcessSheetAsync(activeBroker, resolvedPortfolioName, fileName, fileId, spreadsheet, progress);
            }

            if (sheetIndex < activeSheets.Count - 1)
            {
                progress?.Report($"[{fileName}] Waiting {DelayBetweenSheetsMs / 1000} seconds before next sheet...");
                await Task.Delay(DelayBetweenSheetsMs);
            }
        }
    }

    private static Broker CreateBroker(Investments data, string fileName, string currency, bool isHistoric)
    {
        var broker = Broker.Create(fileName, currency);
        if (isHistoric)
        {
            data.AddHistoricBroker(broker);
        }
        else
        {
            data.AddActiveBroker(broker);
        }
        return broker;
    }

    private async Task ProcessSheetAsync(Broker broker, string portfolioName, string fileName, string fileId, DTO.SheetDTO spreadsheet, IProgress<string> progress)
    {
        var portfolio = broker.AddPortfolio(portfolioName);

        var assetData = await _metadataResolver.ResolveAssetMetadataAsync(fileName, fileId, spreadsheet.Name);
        var asset = Asset.Create(
            spreadsheet.Name,
            assetData.isin,
            assetData.exchangeId,
            assetData.ticker,
            assetData.country,
            assetData.localTypeCode,
            assetData.assetClass);
        portfolio.AddAsset(asset);

        asset.AddTransactions(await _sheetsReader.ReadTransactionsAsync(fileId, spreadsheet.Name));
        await Task.Delay(DelayBetweenOperationsMs);
        asset.AddCredits(await _sheetsReader.ReadCreditsAsync(fileId, spreadsheet.Name));
    }
}
