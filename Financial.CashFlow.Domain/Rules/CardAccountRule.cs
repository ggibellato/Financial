using System;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class CardAccountRule
{
    public static void EnsureIsCard(Account account)
    {
        if (!account.Has(Capability.HasStatements))
        {
            throw new ArgumentException($"{account.Name} is not a card account.");
        }
    }
}
