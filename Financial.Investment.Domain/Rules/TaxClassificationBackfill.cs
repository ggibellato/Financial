using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record TaxClassificationBackfillFailure(Guid SourceId, string Message);

public static class TaxClassificationBackfill
{
    public static IReadOnlyList<TaxClassificationBackfillFailure> Apply(Investments investments)
    {
        var failures = new List<TaxClassificationBackfillFailure>();

        foreach (var broker in investments.ActiveBrokers.Concat(investments.HistoricBrokers))
        {
            foreach (var portfolio in broker.Portfolios)
            {
                foreach (var asset in portfolio.Assets)
                {
                    ApplyToAsset(asset, investments, failures);
                }
            }
        }

        return failures;
    }

    private static void ApplyToAsset(Asset asset, Investments investments, List<TaxClassificationBackfillFailure> failures)
    {
        var classifiedDisposalIds = asset.TaxClassifications
            .Where(c => c.SourceType == SourceType.Disposal)
            .Select(c => c.SourceId)
            .ToHashSet();

        foreach (var record in asset.DisposalRecords.Where(r => r.Status == DisposalRecordStatus.Active))
        {
            if (classifiedDisposalIds.Contains(record.Id))
            {
                continue;
            }

            try
            {
                asset.AppendTaxClassification(TaxClassificationCalculator.CalculateForDisposal(record, investments));
            }
            catch (Exception ex)
            {
                failures.Add(new TaxClassificationBackfillFailure(record.Id, ex.Message));
            }
        }

        var classifiedCreditIds = asset.TaxClassifications
            .Where(c => c.SourceType == SourceType.Credit)
            .Select(c => c.SourceId)
            .ToHashSet();

        foreach (var credit in asset.Credits)
        {
            if (classifiedCreditIds.Contains(credit.Id))
            {
                continue;
            }

            try
            {
                asset.AppendTaxClassification(TaxClassificationCalculator.CalculateForCredit(credit, investments));
            }
            catch (Exception ex)
            {
                failures.Add(new TaxClassificationBackfillFailure(credit.Id, ex.Message));
            }
        }
    }
}
