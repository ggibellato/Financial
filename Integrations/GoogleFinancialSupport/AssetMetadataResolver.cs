using Financial.Domain.Entities;
using Financial.Infrastructure.Integrations.GoogleFinancialSupport.DTO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

internal sealed class AssetMetadataResolver
{
    private const string DefaultPortfolioName = "Default";
    private const string XpiBrokerName = "XPI";
    private const string XpiExchangeId = "BVMF";

    private readonly GoogleGeneratorOptions _options;
    private readonly GoogleSheetsAssetReader _sheetsReader;

    internal AssetMetadataResolver(GoogleGeneratorOptions options, GoogleSheetsAssetReader sheetsReader)
    {
        _options = options;
        _sheetsReader = sheetsReader;
    }

    internal bool IsIgnoredSheet(string sheetName) =>
        _options.IgnoreSheetNames.Contains(sheetName);

    internal string ResolveBrokerCurrency(string brokerName)
    {
        if (_options.BrokerCurrencyMap.TryGetValue(brokerName, out var currency))
            return currency;
        throw new InvalidOperationException(
            $"No currency mapping found for broker '{brokerName}'. Add it to BrokerCurrencyMap.");
    }

    internal string ResolvePortfolioName(string brokerName, SheetDTO spreadsheet)
    {
        var portfolioName = string.IsNullOrWhiteSpace(spreadsheet.Color) ? DefaultPortfolioName : spreadsheet.Color;
        if (_options.PortfolioNameMap.TryGetValue($"{brokerName}_{portfolioName}", out var name))
        {
            portfolioName = name;
        }
        return portfolioName;
    }

    internal async Task<(string isin, string exchangeId, string ticker, CountryCode country, string localTypeCode, GlobalAssetClass assetClass)> ResolveAssetMetadataAsync(
        string brokerName,
        string fileId,
        string spreadsheetName)
    {
        var baseData = brokerName == XpiBrokerName
            ? (isin: string.Empty, exchangeId: XpiExchangeId, ticker: spreadsheetName)
            : await _sheetsReader.GetAssetDataAsync(fileId, spreadsheetName);

        var brokerCountry = ResolveBrokerCountry(brokerName);
        var classification = ResolveAssetClassification(spreadsheetName, brokerCountry);
        var country = classification.Country != CountryCode.Unknown ? classification.Country : brokerCountry;

        return (baseData.isin, baseData.exchangeId, baseData.ticker, country, classification.LocalTypeCode, classification.Class);
    }

    private CountryCode ResolveBrokerCountry(string brokerName)
    {
        if (!_options.BrokerCurrencyMap.TryGetValue(brokerName, out var currency))
        {
            return CountryCode.Unknown;
        }

        return CountryCodeResolver.FromCurrency(currency);
    }

    private static AssetClassificationEntry ResolveAssetClassification(string assetName, CountryCode brokerCountry)
    {
        if (AssetClassificationLookup.TryGet(assetName, out var classification))
        {
            return classification;
        }

        return new AssetClassificationEntry(brokerCountry, string.Empty, GlobalAssetClass.Unknown);
    }
}
