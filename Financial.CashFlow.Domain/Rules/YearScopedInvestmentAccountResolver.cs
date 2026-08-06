using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Domain.Rules;

/// <summary>
/// Determines which investment accounts apply to a given year. For the current, in-progress
/// year (or any later year), every active account applies, so a new month can still be tracked
/// by hand before it's ever been imported. For any past year, an account applies only if it has
/// at least one persisted snapshot that year - existence is derived purely from snapshot
/// presence, which F03's explicit-zero-for-blank-months import guarantees is complete for every
/// matched account-year, independent of the account's current active/disabled status.
/// </summary>
public static class YearScopedInvestmentAccountResolver
{
    public static IReadOnlyList<InvestmentAccount> ResolveForYear(
        IReadOnlyCollection<InvestmentAccount> accounts,
        IReadOnlyCollection<InvestmentSnapshot> snapshots,
        int year,
        int currentYear)
    {
        if (year >= currentYear)
        {
            return accounts.Where(a => a.IsActive).ToList();
        }

        var idsWithSnapshotsThatYear = snapshots
            .Where(s => s.Year == year)
            .Select(s => s.Account.Id)
            .ToHashSet();

        return accounts.Where(a => idsWithSnapshotsThatYear.Contains(a.Id)).ToList();
    }
}
