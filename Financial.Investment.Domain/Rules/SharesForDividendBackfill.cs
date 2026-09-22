using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record SharesForDividendBackfillFailure(Guid CreditId, string Message);

// Uses the position held the day before the credit's date, not the current position, which may
// include shares bought later.
public static class SharesForDividendBackfill
{
    private static readonly HashSet<Credit.CreditType> BackfillableTypes = [Credit.CreditType.Dividend, Credit.CreditType.JCP];

    public static IReadOnlyList<SharesForDividendBackfillFailure> Apply(Investments investments)
    {
        var failures = new List<SharesForDividendBackfillFailure>();

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

    private static void ApplyToAsset(Asset asset, List<SharesForDividendBackfillFailure> failures)
    {
        foreach (var credit in asset.Credits)
        {
            if (credit.SharesForDividend is not null || !BackfillableTypes.Contains(credit.Type))
            {
                continue;
            }

            try
            {
                var openLots = OpenLotTracker.GetOpenLots(asset.Transactions.Where(t => t.Date < credit.Date));
                var quantityHeld = openLots.Sum(lot => lot.RemainingQuantity);

                if (quantityHeld <= 0)
                {
                    failures.Add(new SharesForDividendBackfillFailure(credit.Id, $"No shares were held before {credit.Date:yyyy-MM-dd}."));
                    continue;
                }

                credit.BackfillSharesForDividend(quantityHeld);
            }
            catch (Exception ex)
            {
                failures.Add(new SharesForDividendBackfillFailure(credit.Id, ex.Message));
            }
        }
    }
}
