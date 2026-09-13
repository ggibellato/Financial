using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Validation;

namespace Financial.Investment.Application.Services;

/// <summary>
/// Resolves the currency a new Transaction/Credit should record, and - when it differs from the
/// configured reporting currency - captures a one-time FxRateSnapshot for it. Shared by
/// TransactionService and CreditService so both entry paths capture identically.
/// </summary>
internal static class FxEntryCaptureHelper
{
    public static async Task<(Currency Currency, FxRateSnapshot? FxRateSnapshot)?> CaptureAsync(
        IInvestmentRepository repository,
        IExchangeRateProvider exchangeRateProvider,
        IReportingCurrencyProvider reportingCurrencyProvider,
        TimeProvider timeProvider,
        string brokerName,
        DateTime date)
    {
        var broker = repository.GetBrokerList(InvestmentScope.Active)
            .FirstOrDefault(b => string.Equals(b.Name, brokerName, StringComparison.Ordinal));
        if (broker is null)
        {
            return null;
        }

        if (!EnumParser.TryParseEnum<Currency>(broker.Currency, out var currency))
        {
            throw new ArgumentException($"Broker \"{broker.Name}\" has an unrecognized currency \"{broker.Currency}\".");
        }

        var reportingCurrency = reportingCurrencyProvider.GetReportingCurrency();
        if (currency == reportingCurrency)
        {
            return (currency, null);
        }

        var rate = await exchangeRateProvider.GetHistoricalRateAsync(DateOnly.FromDateTime(date), currency, reportingCurrency)
            .ConfigureAwait(false);

        var snapshot = rate.HasValue
            ? FxRateSnapshot.Create(reportingCurrency, rate.Value, FxRateSource.Frankfurter, timeProvider.GetUtcNow())
            : null;

        return (currency, snapshot);
    }
}
