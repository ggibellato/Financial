using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public sealed class StubReportingCurrencyProvider : IReportingCurrencyProvider
{
    private Currency _currency;

    public StubReportingCurrencyProvider(Currency currency = Currency.GBP)
    {
        _currency = currency;
    }

    public Currency GetReportingCurrency() => _currency;

    public Task SetReportingCurrencyAsync(Currency currency)
    {
        _currency = currency;
        return Task.CompletedTask;
    }
}
