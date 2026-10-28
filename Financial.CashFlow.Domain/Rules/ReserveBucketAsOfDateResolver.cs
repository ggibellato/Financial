using System;

namespace Financial.CashFlow.Domain.Rules;

public static class ReserveBucketAsOfDateResolver
{
    public static DateOnly LastDayOfPriorMonth(int year, int month) =>
        new DateOnly(year, month, 1).AddDays(-1);
}
