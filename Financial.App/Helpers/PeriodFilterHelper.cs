namespace Financial.Presentation.App.Helpers;

public enum PeriodFilter
{
    ThisMonth,
    Last3Months,
    Last6Months,
    Last12Months,
    Ytd,
    AllTime
}

public static class PeriodFilterHelper
{
    public static readonly IReadOnlyList<(string Label, PeriodFilter Filter)> Options = new (string, PeriodFilter)[]
    {
        ("This month", PeriodFilter.ThisMonth),
        ("Last 3 months", PeriodFilter.Last3Months),
        ("Last 6 months", PeriodFilter.Last6Months),
        ("Last 12 months", PeriodFilter.Last12Months),
        ("YTD", PeriodFilter.Ytd),
        ("All time", PeriodFilter.AllTime),
    };

    public static (DateTime? Start, DateTime? EndExclusive) GetDateRange(PeriodFilter filter, DateTime referenceDate)
    {
        if (filter == PeriodFilter.AllTime)
            return (null, null);

        var currentMonthStart = new DateTime(referenceDate.Year, referenceDate.Month, 1);
        var monthsBack = filter switch
        {
            PeriodFilter.ThisMonth => 0,
            PeriodFilter.Last3Months => 2,
            PeriodFilter.Last6Months => 5,
            PeriodFilter.Last12Months => 11,
            PeriodFilter.Ytd => referenceDate.Month - 1,
            _ => 0
        };

        var start = currentMonthStart.AddMonths(-monthsBack);
        var endExclusive = currentMonthStart.AddMonths(1);
        return (start, endExclusive);
    }
}
