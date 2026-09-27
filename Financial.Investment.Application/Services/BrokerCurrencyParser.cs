using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Validation;

namespace Financial.Investment.Application.Services;

internal static class BrokerCurrencyParser
{
    public static Currency Parse(string rawCurrency)
    {
        if (!EnumParser.TryParseEnum<Currency>(rawCurrency, out var currency))
        {
            throw new ArgumentException($"Broker currency \"{rawCurrency}\" is not recognized.", nameof(rawCurrency));
        }

        return currency;
    }

    public static bool Matches(Currency brokerCurrency, Currency? filter) =>
        filter is null || brokerCurrency == filter.Value;
}
