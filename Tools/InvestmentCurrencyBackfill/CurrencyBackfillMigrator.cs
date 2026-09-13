using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Validation;

namespace Financial.Investment.CurrencyBackfill;

/// <summary>
/// Backfills Currency and FxRateSnapshot on every existing Transaction and Credit, per P49 F02.
/// Idempotent: a record whose Currency already matches its broker, and whose FxRateSnapshot is
/// already populated (or legitimately not needed, because the broker's currency already matches
/// the reporting currency), is left untouched. A record whose snapshot is still null after a
/// prior run - because no rate was obtainable - is retried on every subsequent run, exactly like
/// F01's own no-rate-anywhere-in-the-window case.
/// </summary>
public static class CurrencyBackfillMigrator
{
    public static async Task<CurrencyBackfillSummary> MigrateAsync(
        Investments data, IExchangeRateProvider exchangeRateProvider, Currency reportingCurrency)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(exchangeRateProvider);

        var summary = new CurrencyBackfillSummary();

        foreach (var broker in data.ActiveBrokers.Concat(data.HistoricBrokers))
        {
            if (!EnumParser.TryParseEnum<Currency>(broker.Currency, out var brokerCurrency))
            {
                summary.FlagUnresolvedBroker($"{broker.Name}: unrecognized currency \"{broker.Currency}\"");
                continue;
            }

            foreach (var portfolio in broker.Portfolios)
            {
                foreach (var asset in portfolio.Assets)
                {
                    await MigrateTransactionsAsync(broker.Name, asset, brokerCurrency, exchangeRateProvider, reportingCurrency, summary).ConfigureAwait(false);
                    await MigrateCreditsAsync(broker.Name, asset, brokerCurrency, exchangeRateProvider, reportingCurrency, summary).ConfigureAwait(false);
                }
            }
        }

        return summary;
    }

    private static async Task MigrateTransactionsAsync(
        string brokerName, Asset asset, Currency brokerCurrency, IExchangeRateProvider exchangeRateProvider, Currency reportingCurrency, CurrencyBackfillSummary summary)
    {
        foreach (var transaction in asset.Transactions.ToList())
        {
            var needsWork = transaction.Currency != brokerCurrency
                || (transaction.FxRateSnapshot is null && brokerCurrency != reportingCurrency);
            if (!needsWork)
            {
                summary.CountTransactionAlreadySet();
                continue;
            }

            if (transaction.Id == Guid.Empty)
            {
                summary.FlagUnaddressableRecord($"{brokerName}/{asset.Name} transaction {transaction.Date:yyyy-MM-dd} has no Id");
                continue;
            }

            var snapshot = await CaptureSnapshotAsync(
                brokerCurrency, reportingCurrency, transaction.Date, exchangeRateProvider).ConfigureAwait(false);

            if (snapshot is null && brokerCurrency != reportingCurrency)
            {
                summary.FlagUnresolvedTransaction($"{brokerName}/{asset.Name} {transaction.Date:yyyy-MM-dd} {transaction.Id}");
            }

            var replacement = Transaction.CreateWithId(
                transaction.Id, transaction.Date, transaction.Type, transaction.Quantity, transaction.UnitPrice,
                transaction.Fees, transaction.Withheld, brokerCurrency, snapshot);
            if (!asset.UpdateTransaction(replacement))
            {
                summary.FlagUnaddressableRecord($"{brokerName}/{asset.Name} transaction {transaction.Date:yyyy-MM-dd} {transaction.Id} could not be located for update");
                continue;
            }

            summary.CountTransactionBackfilled();
        }
    }

    private static async Task MigrateCreditsAsync(
        string brokerName, Asset asset, Currency brokerCurrency, IExchangeRateProvider exchangeRateProvider, Currency reportingCurrency, CurrencyBackfillSummary summary)
    {
        foreach (var credit in asset.Credits.ToList())
        {
            var needsWork = credit.Currency != brokerCurrency
                || (credit.FxRateSnapshot is null && brokerCurrency != reportingCurrency);
            if (!needsWork)
            {
                summary.CountCreditAlreadySet();
                continue;
            }

            if (credit.Id == Guid.Empty)
            {
                summary.FlagUnaddressableRecord($"{brokerName}/{asset.Name} credit {credit.Date:yyyy-MM-dd} has no Id");
                continue;
            }

            var snapshot = await CaptureSnapshotAsync(
                brokerCurrency, reportingCurrency, credit.Date, exchangeRateProvider).ConfigureAwait(false);

            if (snapshot is null && brokerCurrency != reportingCurrency)
            {
                summary.FlagUnresolvedCredit($"{brokerName}/{asset.Name} {credit.Date:yyyy-MM-dd} {credit.Id}");
            }

            var replacement = Credit.CreateWithId(
                credit.Id, credit.Date, credit.Type, credit.Value, credit.Withheld, brokerCurrency, snapshot);
            if (!asset.UpdateCredit(replacement))
            {
                summary.FlagUnaddressableRecord($"{brokerName}/{asset.Name} credit {credit.Date:yyyy-MM-dd} {credit.Id} could not be located for update");
                continue;
            }

            summary.CountCreditBackfilled();
        }
    }

    private static async Task<FxRateSnapshot?> CaptureSnapshotAsync(
        Currency brokerCurrency, Currency reportingCurrency, DateTime date, IExchangeRateProvider exchangeRateProvider)
    {
        if (brokerCurrency == reportingCurrency)
        {
            return null;
        }

        var rate = await exchangeRateProvider.GetHistoricalRateAsync(DateOnly.FromDateTime(date), brokerCurrency, reportingCurrency)
            .ConfigureAwait(false);

        return rate.HasValue
            ? FxRateSnapshot.Create(reportingCurrency, rate.Value, FxRateSource.Frankfurter, DateTimeOffset.UtcNow)
            : null;
    }
}
