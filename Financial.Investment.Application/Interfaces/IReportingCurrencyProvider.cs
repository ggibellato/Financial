using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Interfaces;

/// <summary>
/// The currency entry-time FX capture converts into. A fixed interim implementation until P49 F03
/// replaces it with the real persisted reporting-currency setting.
/// </summary>
public interface IReportingCurrencyProvider
{
    Currency GetReportingCurrency();
}
