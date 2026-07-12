using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;

namespace Financial.Infrastructure.Services;

public sealed class AssetPriceService : IAssetPriceService
{
    private readonly IRepository _repository;

    public AssetPriceService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Ticker is required.", nameof(request));
        }

        var snapshot = request.AssetClass == GlobalAssetClass.Cryptocurrency
            ? GetCryptocurrencySnapshot(request)
            : GetStandardSnapshot(request);

        return new AssetPriceDTO
        {
            Exchange = request.Exchange,
            Ticker = snapshot.Ticker,
            Name = snapshot.Name,
            Price = snapshot.Price,
            AsOf = snapshot.AsOf
        };
    }

    private static AssetValueSnapshot GetStandardSnapshot(AssetPriceRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Exchange))
        {
            throw new ArgumentException("Exchange is required.", nameof(request));
        }

        return GoogleFinance.GetFinancialInfoSnapshot(request.Exchange, request.Ticker);
    }

    private AssetValueSnapshot GetCryptocurrencySnapshot(AssetPriceRequestDTO request)
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

