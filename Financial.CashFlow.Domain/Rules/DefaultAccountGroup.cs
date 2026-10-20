using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Domain.Rules;

public static class DefaultAccountGroup
{
    public static IReadOnlyList<Account> MembersFor(
        IEnumerable<Account> accounts, int year, int month, IReadOnlyCollection<IAccountTransaction> records)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(records);

        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        return accounts
            .Where(account => account.JoinsDefaultGroup)
            .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .Where(account => account.IsActive || StillMattersIn(account, start, end, records))
            .ToList();
    }

    private static bool StillMattersIn(Account account, DateOnly start, DateOnly end, IReadOnlyCollection<IAccountTransaction> records) =>
        AccountBalanceRule.Balance(account, end, records) != 0m
        || records.Any(record => record.Date >= start && record.Date <= end && record.ContributionTo(account) != 0m);
}
