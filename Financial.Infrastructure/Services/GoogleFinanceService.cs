using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

public sealed class GoogleFinanceService : IFinanceService
{
    private readonly Func<string, string, AssetValueSnapshot> _exchangeLookup;
    private readonly Func<string, string, AssetValueSnapshot> _cryptoLookup;

    public GoogleFinanceService()
        : this(GoogleFinance.GetFinancialInfoSnapshot, GoogleFinance.GetCryptocurrencyFinancialInfoSnapshot)
    {
    }

    internal GoogleFinanceService(
        Func<string, string, AssetValueSnapshot> exchangeLookup,
        Func<string, string, AssetValueSnapshot> cryptoLookup)
    {
        _exchangeLookup = exchangeLookup ?? throw new ArgumentNullException(nameof(exchangeLookup));
        _cryptoLookup = cryptoLookup ?? throw new ArgumentNullException(nameof(cryptoLookup));
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Ticker is required.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.Exchange))
        {
            return _exchangeLookup(request.Exchange, request.Ticker);
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            return _cryptoLookup(request.Currency, request.Ticker);
        }

        throw new ArgumentException("Either Exchange or Currency must be provided.", nameof(request));
    }
}
