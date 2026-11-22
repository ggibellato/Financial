using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record RealisedGainBackfillFailure(Guid TransactionId, string Message);

public static class RealisedGainBackfill
{
    public static IReadOnlyList<RealisedGainBackfillFailure> Apply(Investments investments, DateTimeOffset createdAt)
    {
        var failures = new List<RealisedGainBackfillFailure>();

        foreach (var broker in investments.ActiveBrokers.Concat(investments.HistoricBrokers))
        {
            foreach (var portfolio in broker.Portfolios)
            {
                foreach (var asset in portfolio.Assets)
                {
                    ApplyToAsset(asset, createdAt, failures);
                }
            }
        }

        return failures;
    }

    private static void ApplyToAsset(Asset asset, DateTimeOffset createdAt, List<RealisedGainBackfillFailure> failures)
    {
        var existingTransactionIds = asset.RealisedGains.Select(record => record.TransactionId).ToHashSet();
        var preceding = new List<Transaction>();

        foreach (var transaction in TransactionReplayOrder.Sort(asset.Transactions))
        {
            var effect = TransactionTypeEffects.For(transaction.Type);
            var realisesGain = effect.Quantity == QuantityEffect.Decrease && effect.Cash != CashEffect.None;

            if (realisesGain && !existingTransactionIds.Contains(transaction.Id))
            {
                try
                {
                    var record = RealisedGainCalculator.Calculate(
                        transaction, preceding, CostBasisMethod.AverageCost, transaction.Currency.ToString(), createdAt);
                    asset.AppendBackfilledRealisedGain(record);
                }
                catch (Exception ex)
                {
                    failures.Add(new RealisedGainBackfillFailure(transaction.Id, ex.Message));
                }
            }

            preceding.Add(transaction);
        }

        asset.RefreshRealisedCapitalGain();
    }
}
