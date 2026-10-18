using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Domain.Rules;

public static class AccountBalanceRule
{
    public static decimal Balance(Account account, DateOnly asOf, IEnumerable<IAccountTransaction> records)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(records);

        var openingBalance = account.OpeningBalance;
        var windowStart = openingBalance?.StartDate ?? DateOnly.MinValue;

        return (openingBalance?.Amount ?? 0m)
            + records
                .Where(record => record.Date >= windowStart && record.Date <= asOf)
                .Sum(record => record.ContributionTo(account));
    }
}
