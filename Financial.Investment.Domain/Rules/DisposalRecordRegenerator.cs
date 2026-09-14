using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class DisposalRecordRegenerator
{
    private sealed record RegenerationPlan(
        Asset Asset,
        IReadOnlyList<DisposalRecord> ToRetire,
        IReadOnlyList<(DisposalRecord Existing, DisposalRecord New)> Replacements,
        IReadOnlyList<DisposalRecord> NewOnly);

    public static void RegenerateAsset(
        Asset asset,
        CostBasisMethod method,
        DateTime anchor,
        Guid? seedTransactionId = null,
        IReadOnlyList<SpecificLotAllocation>? seedAllocation = null) =>
        Commit(ComputePlan(asset, method, anchor, seedTransactionId, seedAllocation));

    // Every asset's plan is computed before any is committed, so a throw for any one asset leaves every asset untouched.
    public static void RegenerateBroker(Broker broker)
    {
        var plans = broker.Portfolios
            .SelectMany(portfolio => portfolio.Assets)
            .Select(asset => ComputePlan(asset, broker.CostBasisMethod, DateTime.MinValue))
            .ToList();

        foreach (var plan in plans)
        {
            Commit(plan);
        }
    }

    private static RegenerationPlan ComputePlan(
        Asset asset,
        CostBasisMethod method,
        DateTime anchor,
        Guid? seedTransactionId = null,
        IReadOnlyList<SpecificLotAllocation>? seedAllocation = null)
    {
        var transactionIds = asset.Transactions.Select(t => t.Id).ToHashSet();
        var existingActive = asset.DisposalRecords.Where(r => r.Status == DisposalRecordStatus.Active).ToList();
        var existingByTransactionId = existingActive.ToDictionary(r => r.TransactionId);

        var toRetire = existingActive
            .Where(r => r.Date >= anchor && !transactionIds.Contains(r.TransactionId))
            .ToList();

        var replacements = new List<(DisposalRecord Existing, DisposalRecord New)>();
        var newOnly = new List<DisposalRecord>();
        var preceding = new List<Transaction>();

        foreach (var transaction in TransactionReplayOrder.Sort(asset.Transactions))
        {
            var effect = TransactionTypeEffects.For(transaction.Type);
            var isDisposing = effect.Quantity == QuantityEffect.Decrease && effect.Cash != CashEffect.None;

            if (isDisposing && transaction.Date >= anchor)
            {
                var existing = existingByTransactionId.GetValueOrDefault(transaction.Id);
                var allocation = transaction.Id == seedTransactionId ? seedAllocation : ReconstructAllocation(existing);

                var newRecord = DisposalRecordCalculator.Calculate(
                    transaction, preceding, method, transaction.Currency.ToString(), allocation);

                if (existing is not null)
                {
                    replacements.Add((existing, newRecord));
                }
                else
                {
                    newOnly.Add(newRecord);
                }
            }

            preceding.Add(transaction);
        }

        return new RegenerationPlan(asset, toRetire, replacements, newOnly);
    }

    private static void Commit(RegenerationPlan plan)
    {
        foreach (var record in plan.ToRetire)
        {
            record.Supersede(null);
        }

        foreach (var (existing, newRecord) in plan.Replacements)
        {
            existing.Supersede(newRecord.Id);
            plan.Asset.AppendBackfilledDisposalRecord(newRecord);
        }

        foreach (var newRecord in plan.NewOnly)
        {
            plan.Asset.AppendBackfilledDisposalRecord(newRecord);
        }

        plan.Asset.RefreshRealizedCapitalGain();
    }

    private static IReadOnlyList<SpecificLotAllocation>? ReconstructAllocation(DisposalRecord? record)
    {
        if (record is null || record.Method != CostBasisMethod.SpecificId)
        {
            return null;
        }

        return record.LotsConsumed
            .Where(lot => lot.SourceTransactionId.HasValue)
            .Select(lot => new SpecificLotAllocation(lot.SourceTransactionId!.Value, lot.Quantity))
            .ToList();
    }
}
