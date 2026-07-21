using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;

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
}
