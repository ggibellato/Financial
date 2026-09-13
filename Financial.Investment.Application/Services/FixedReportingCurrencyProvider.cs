using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Services;

/// <summary>
/// Interim <see cref="IReportingCurrencyProvider"/> fixed at GBP until P49 F03 introduces the real
/// persisted reporting-currency setting. GBP matches the currency of three of the four existing
/// brokers and is F03's own stated default.
/// </summary>
public sealed class FixedReportingCurrencyProvider : IReportingCurrencyProvider
{
    public Currency GetReportingCurrency() => Currency.GBP;
}
