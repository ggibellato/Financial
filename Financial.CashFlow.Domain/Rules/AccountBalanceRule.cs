using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Rules;

public static class AccountBalanceRule
{
    public static decimal Balance(Account account, DateOnly asOf, IEnumerable<IAccountTransaction> records)
    {
        ArgumentNullException.ThrowIfNull(account);
        return Balance(account, account.Currency, asOf, records);
    }

    public static decimal Balance(Account account, Currency currency, DateOnly asOf, IEnumerable<IAccountTransaction> records)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(records);

        var openingBalance = account.OpeningBalance;
        var windowStart = openingBalance?.StartDate ?? DateOnly.MinValue;
        var openingAmount = currency == account.Currency ? openingBalance?.Amount ?? 0m : 0m;

        return openingAmount
            + records
                .Where(record => record.Date >= windowStart && record.Date <= asOf)
                .Sum(record => record.ContributionTo(account, currency));
    }

    public static IReadOnlyList<(Currency Currency, decimal Balance)> Balances(
        Account account, DateOnly asOf, IReadOnlyCollection<IAccountTransaction> records)
    {
        ArgumentNullException.ThrowIfNull(account);
        return account.Currencies.Select(currency => (currency, Balance(account, currency, asOf, records))).ToList();
    }

    public static decimal PotBalance(Pot pot, DateOnly asOf, IEnumerable<PotTransaction> movements)
    {
        ArgumentNullException.ThrowIfNull(pot);
        ArgumentNullException.ThrowIfNull(movements);

        return movements
            .Where(movement => movement.Pot.Id == pot.Id && movement.Date <= asOf)
            .Sum(movement => movement.Amount);
    }
}
