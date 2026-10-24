using System;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Domain.Rules;

public static class ActiveAccountRule
{
    public static void EnsureUsable(Account? current, Account? candidate)
    {
        if (candidate is not null && candidate.Id != current?.Id && !candidate.IsActive)
        {
            throw new ArgumentException($"{candidate.Name} is inactive.");
        }
    }
}
