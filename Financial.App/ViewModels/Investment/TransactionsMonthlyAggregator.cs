using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels.Investment;

internal sealed record TransactionMonthNet(DateTime Month, decimal NetInvested);

internal static class TransactionsMonthlyAggregator
{
    /// <summary>
    /// Net invested for a month is the money that left the investor's pocket for these holdings
    /// minus the money that came back - the negation of <c>NetCash</c> (which is signed the other
    /// way: negative for an outflow, positive for an inflow), so no per-type branching is needed
    /// here: it falls out of every type's own declared cash effect.
    /// </summary>
    public static IReadOnlyList<TransactionMonthNet> BuildMonthlyNetInvested(
        IEnumerable<(DateTime Date, decimal NetCash)> transactions,
        PeriodFilter filter,
        DateTime referenceDate)
    {
        var items = transactions.ToList();

        var netByMonth = new Dictionary<DateTime, decimal>();
        foreach (var (date, netCash) in items)
        {
            var month = StartOfMonth(date);
            netByMonth[month] = netByMonth.GetValueOrDefault(month) - netCash;
        }

        var (periodStart, _) = PeriodFilterHelper.GetDateRange(filter, referenceDate);
        var rangeStart = periodStart.HasValue
            ? StartOfMonth(periodStart.Value)
            : items.Count > 0
                ? StartOfMonth(items.Min(t => t.Date))
                : StartOfMonth(referenceDate);

        var rangeEnd = StartOfMonth(referenceDate);
        var buckets = new List<TransactionMonthNet>();
        for (var cursor = rangeStart; cursor <= rangeEnd; cursor = cursor.AddMonths(1))
        {
            buckets.Add(new TransactionMonthNet(cursor, netByMonth.GetValueOrDefault(cursor)));
        }

        return buckets;
    }

    private static DateTime StartOfMonth(DateTime date) => new(date.Year, date.Month, 1);
}
