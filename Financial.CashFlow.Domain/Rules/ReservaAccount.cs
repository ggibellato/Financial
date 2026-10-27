using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class ReservaAccount
{
    public static Account? Find(IEnumerable<Account> accounts) =>
        accounts.FirstOrDefault(account => account.Has(Capability.HasPots));
}
