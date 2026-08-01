using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels;

internal sealed record TransactionMonthNet(DateTime Month, decimal NetInvested);

internal static class TransactionsMonthlyAggregator
{
    private const string BuyType = "Buy";

    public static IReadOnlyList<TransactionMonthNet> BuildMonthlyNetInvested(
        IEnumerable<(DateTime Date, string Type, decimal TotalPrice)> transactions,
        PeriodFilter filter,
        DateTime referenceDate)
    {
        var items = transactions.ToList();

        var netByMonth = new Dictionary<DateTime, decimal>();
        foreach (var (date, type, totalPrice) in items)
        {
            var month = StartOfMonth(date);
            var delta = string.Equals(type, BuyType, StringComparison.OrdinalIgnoreCase) ? totalPrice : -totalPrice;
            netByMonth[month] = netByMonth.GetValueOrDefault(month) + delta;
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
