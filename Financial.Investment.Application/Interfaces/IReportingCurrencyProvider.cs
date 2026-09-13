using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Interfaces;

/// <summary>
/// Backed by the persisted <see cref="Financial.Investment.Domain.Entities.Investments.ReportingCurrency"/> setting.
/// </summary>
public interface IReportingCurrencyProvider
{
    Currency GetReportingCurrency();

    Task SetReportingCurrencyAsync(Currency currency);

    bool IsReportingCurrencyEnabled();

    Task SetReportingCurrencyEnabledAsync(bool enabled);
}
