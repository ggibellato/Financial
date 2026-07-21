using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Services;

public sealed class CryptocurrencyAssetPriceFetcher : IAssetPriceFetcher
{
    private readonly IRepository _repository;
    private readonly IFinanceService _financeService;

    public CryptocurrencyAssetPriceFetcher(IRepository repository, IFinanceService financeService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _financeService = financeService ?? throw new ArgumentNullException(nameof(financeService));
    }

    public bool Supports(GlobalAssetClass assetClass) => assetClass == GlobalAssetClass.Cryptocurrency;

    public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.BrokerName))
        {
            throw new ArgumentException("BrokerName is required for cryptocurrency assets.", nameof(request));
        }

        var currency = ResolveBrokerCurrency(_repository.GetBrokerList(), request.BrokerName);
        return _financeService.GetAssetValue(new AssetValueRequestDTO { Currency = currency, Ticker = request.Ticker });
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
