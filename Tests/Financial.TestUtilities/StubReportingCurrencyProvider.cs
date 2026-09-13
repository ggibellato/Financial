using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public sealed class StubReportingCurrencyProvider : IReportingCurrencyProvider
{
    private Currency _currency;
    private bool _enabled;

    public StubReportingCurrencyProvider(Currency currency = Currency.GBP, bool enabled = true)
    {
        _currency = currency;
        _enabled = enabled;
    }

    public Exception? ThrowOnSetReportingCurrencyAsync { get; set; }
    public Exception? ThrowOnSetReportingCurrencyEnabledAsync { get; set; }

    public Currency GetReportingCurrency() => _currency;

    public Task SetReportingCurrencyAsync(Currency currency)
    {
        if (ThrowOnSetReportingCurrencyAsync is not null)
        {
            throw ThrowOnSetReportingCurrencyAsync;
        }

        _currency = currency;
        return Task.CompletedTask;
    }

    public bool IsReportingCurrencyEnabled() => _enabled;

    public Task SetReportingCurrencyEnabledAsync(bool enabled)
    {
        if (ThrowOnSetReportingCurrencyEnabledAsync is not null)
        {
            throw ThrowOnSetReportingCurrencyEnabledAsync;
        }

        _enabled = enabled;
        return Task.CompletedTask;
    }
}
