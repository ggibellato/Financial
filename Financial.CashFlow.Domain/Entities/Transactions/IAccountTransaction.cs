using System;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public interface IAccountTransaction
{
    DateOnly Date { get; }

    decimal ContributionTo(Account account);
}
