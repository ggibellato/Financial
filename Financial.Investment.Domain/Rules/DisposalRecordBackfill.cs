using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record DisposalRecordBackfillFailure(Guid TransactionId, string Message);

public static class DisposalRecordBackfill
{
    public static IReadOnlyList<DisposalRecordBackfillFailure> Apply(Investments investments)
    {
        var failures = new List<DisposalRecordBackfillFailure>();

        foreach (var broker in investments.ActiveBrokers.Concat(investments.HistoricBrokers))
        {
            foreach (var portfolio in broker.Portfolios)
            {
                foreach (var asset in portfolio.Assets)
                {
                    ApplyToAsset(asset, failures);
                }
            }
        }

        return failures;
    }

    private static void ApplyToAsset(Asset asset, List<DisposalRecordBackfillFailure> failures)
    {
        var existingTransactionIds = asset.DisposalRecords.Select(record => record.TransactionId).ToHashSet();
        var preceding = new List<Transaction>();

        foreach (var transaction in TransactionReplayOrder.Sort(asset.Transactions))
        {
            var effect = TransactionTypeEffects.For(transaction.Type);
            var isDisposing = effect.Quantity == QuantityEffect.Decrease && effect.Cash != CashEffect.None;

            if (isDisposing && !existingTransactionIds.Contains(transaction.Id))
            {
                try
                {
                    var record = DisposalRecordCalculator.Calculate(
                        transaction, preceding, CostBasisMethod.AverageCost, transaction.Currency.ToString());
                    asset.AppendBackfilledDisposalRecord(record);
                }
                catch (Exception ex)
                {
                    failures.Add(new DisposalRecordBackfillFailure(transaction.Id, ex.Message));
                }
            }

            preceding.Add(transaction);
        }

        asset.RefreshRealizedCapitalGain();
    }
}
