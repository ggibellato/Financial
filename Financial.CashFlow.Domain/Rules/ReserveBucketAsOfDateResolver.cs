using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Domain.Rules;

/// <summary>
/// Computes the combined reserve bucket total "as of" a point in time, used by a
/// ReserveBucketsSum-sourced InvestmentAccount's suggested snapshot value: the total as it stood
/// on the last day of the month immediately before the selected one ("how the month started"),
/// across every bucket regardless of active/inactive status - matching F01's "always the
/// combined total, never a specific bucket" rule.
/// </summary>
public static class ReserveBucketAsOfDateResolver
{
    public static DateOnly LastDayOfPriorMonth(int year, int month) =>
        new DateOnly(year, month, 1).AddDays(-1);

    public static decimal TotalBalanceAsOf(IReadOnlyCollection<ReserveMovement> movements, DateOnly asOfDate) =>
        movements.Where(m => m.Date <= asOfDate).Sum(m => m.Amount);
}
