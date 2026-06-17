using Financial.Domain.Entities;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

internal static class CountryCodeResolver
{
    internal static CountryCode FromCurrency(string currency) =>
        currency.ToUpperInvariant() switch
        {
            "BRL" => CountryCode.BR,
            "GBP" => CountryCode.UK,
            "USD" => CountryCode.US,
            _ => CountryCode.Unknown
        };

    internal static CountryCode FromExchange(string exchange)
    {
        if (string.IsNullOrWhiteSpace(exchange))
        {
            return CountryCode.Unknown;
        }

        return exchange.Trim().ToUpperInvariant() switch
        {
            "BVMF" => CountryCode.BR,
            "LON" or "LSE" => CountryCode.UK,
            "NYSE" or "NASDAQ" or "AMEX" => CountryCode.US,
            _ => CountryCode.Unknown
        };
    }
}
