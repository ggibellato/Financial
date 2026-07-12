using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

public sealed class GoogleFinanceService : IFinanceService
{
    public AssetValueSnapshot GetQuote(FinanceQuoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Ticker))
        {
            throw new ArgumentException("Ticker is required.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.Exchange))
        {
            return GoogleFinance.GetFinancialInfoSnapshot(request.Exchange, request.Ticker);
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            return GoogleFinance.GetCryptocurrencyFinancialInfoSnapshot(request.Currency, request.Ticker);
        }

        throw new ArgumentException("Either Exchange or Currency must be provided.", nameof(request));
    }
}
