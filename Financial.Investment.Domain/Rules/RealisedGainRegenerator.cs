using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class RealisedGainRegenerator
{
    private sealed record RegenerationPlan(
        Asset Asset,
        IReadOnlyList<RealisedGain> ToRetire,
        IReadOnlyList<(RealisedGain Existing, RealisedGain New)> Replacements,
        IReadOnlyList<RealisedGain> NewOnly);

    public static void RegenerateAsset(
        Asset asset,
        CostBasisMethod method,
        DateTime anchor,
        DateTimeOffset createdAt,
        Guid? seedTransactionId = null,
        IReadOnlyList<SpecificLotAllocation>? seedAllocation = null,
        Investments? investments = null) =>
        Commit(ComputePlan(asset, method, anchor, createdAt, seedTransactionId, seedAllocation), createdAt, investments);

    // Every asset's plan is computed before any is committed, so a throw for any one asset leaves every asset untouched.
    public static void RegenerateBroker(Broker broker, DateTimeOffset createdAt, Investments? investments = null)
    {
        var plans = broker.Portfolios
            .SelectMany(portfolio => portfolio.Assets)
            .Select(asset => ComputePlan(asset, broker.CostBasisMethod, DateTime.MinValue, createdAt))
            .ToList();

        foreach (var plan in plans)
        {
            Commit(plan, createdAt, investments);
        }
    }

    private static RegenerationPlan ComputePlan(
        Asset asset,
        CostBasisMethod method,
        DateTime anchor,
        DateTimeOffset createdAt,
        Guid? seedTransactionId = null,
        IReadOnlyList<SpecificLotAllocation>? seedAllocation = null)
    {
        var transactionIds = asset.Transactions.Select(t => t.Id).ToHashSet();
        var existingActive = asset.RealisedGains.Where(r => r.Status == RealisedGainStatus.Active).ToList();
        var existingByTransactionId = existingActive.ToDictionary(r => r.TransactionId);

        var toRetire = existingActive
            .Where(r => r.Date >= anchor && !transactionIds.Contains(r.TransactionId))
            .ToList();

        var replacements = new List<(RealisedGain Existing, RealisedGain New)>();
        var newOnly = new List<RealisedGain>();
        var preceding = new List<Transaction>();
        var precedingCorporateActions = new List<CorporateAction>();

        foreach (var step in CorporateActionReplay.Merge(asset.Transactions, asset.CorporateActions))
        {
            switch (step)
            {
                case CorporateActionReplayStep(var corporateAction):
                    precedingCorporateActions.Add(corporateAction);
                    break;

                case TransactionReplayStep(var transaction):
                    var effect = TransactionTypeEffects.For(transaction.Type);
                    var realisesGain = effect.Quantity == QuantityEffect.Decrease && effect.Cash != CashEffect.None;

                    if (realisesGain && transaction.Date >= anchor)
                    {
                        var existing = existingByTransactionId.GetValueOrDefault(transaction.Id);
                        var allocation = transaction.Id == seedTransactionId ? seedAllocation : ReconstructAllocation(existing);

                        var newRecord = RealisedGainCalculator.Calculate(
                            transaction, preceding, method, transaction.Currency.ToString(), createdAt, allocation, precedingCorporateActions);

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
                    break;
            }
        }

        return new RegenerationPlan(asset, toRetire, replacements, newOnly);
    }

    private static void Commit(RegenerationPlan plan, DateTimeOffset createdAt, Investments? investments)
    {
        foreach (var record in plan.ToRetire)
        {
            record.Supersede(null);
            if (investments is not null)
            {
                plan.Asset.SupersedeTaxClassificationBySource(SourceType.RealisedGain, record.Id, null);
            }
        }

        foreach (var (existing, newRecord) in plan.Replacements)
        {
            existing.Supersede(newRecord.Id);
            plan.Asset.AppendBackfilledRealisedGain(newRecord);
            if (investments is not null)
            {
                var newClassification = TaxClassificationCalculator.CalculateForRealisedGain(newRecord, investments, createdAt);
                plan.Asset.SupersedeTaxClassificationBySource(SourceType.RealisedGain, existing.Id, newClassification.Id);
                plan.Asset.AppendTaxClassification(newClassification);
            }
        }

        foreach (var newRecord in plan.NewOnly)
        {
            plan.Asset.AppendBackfilledRealisedGain(newRecord);
            if (investments is not null)
            {
                plan.Asset.AppendTaxClassification(TaxClassificationCalculator.CalculateForRealisedGain(newRecord, investments, createdAt));
            }
        }

        plan.Asset.RefreshRealisedCapitalGain();
    }

    private static IReadOnlyList<SpecificLotAllocation>? ReconstructAllocation(RealisedGain? record)
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
