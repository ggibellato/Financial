using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

public sealed class CryptocurrencyAssetPriceFetcher : IAssetPriceFetcher
{
    private readonly IRepository _repository;

    public CryptocurrencyAssetPriceFetcher(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public bool Supports(GlobalAssetClass assetClass) => assetClass == GlobalAssetClass.Cryptocurrency;

    public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.BrokerName))
        {
            throw new ArgumentException("BrokerName is required for cryptocurrency assets.", nameof(request));
        }

        var currency = ResolveBrokerCurrency(_repository.GetBrokerList(), request.BrokerName);
        return GoogleFinance.GetCryptocurrencyFinancialInfoSnapshot(currency, request.Ticker);
    }

    internal static string ResolveBrokerCurrency(IEnumerable<Broker> brokers, string brokerName)
    {
        var broker = brokers.FirstOrDefault(b => b.Name == brokerName);
        if (broker is null)
        {
            throw new InvalidOperationException($"Broker '{brokerName}' not found.");
        }

        return broker.Currency;
    }
}
