using System;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public interface IAccountTransaction
{
    DateOnly Date { get; }

    decimal ContributionTo(Account account);

    decimal ContributionTo(Account account, Currency currency) =>
        currency == account.Currency ? ContributionTo(account) : 0m;
}
