using System;

namespace Financial.Investment.Domain.Rules;

public static class MarketStatusCalculator
{
    public static MarketStatus For(DateOnly priceDate, DateOnly asOfDate) =>
        priceDate < PreviousWeekday(asOfDate) ? MarketStatus.Stale : MarketStatus.Current;

    private static DateOnly PreviousWeekday(DateOnly date)
    {
        var previous = date.AddDays(-1);
        while (previous.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            previous = previous.AddDays(-1);
        }

        return previous;
    }
}
