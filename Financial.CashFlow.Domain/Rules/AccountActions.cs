using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class AccountActions
{
    private static readonly IReadOnlyList<(AccountAction Action, Func<Account, bool> IsAllowed)> ActionRules =
    [
        (AccountAction.AddExpense, account => account.CanPay),
        (AccountAction.AddCharge, account => account.Has(Capability.HasStatements)),
        (AccountAction.AddIncome, account => account.Has(Capability.ReceivesIncome)),
        (AccountAction.TransferTo, account => account.Has(Capability.CanSendTransfers)),
        (AccountAction.TransferFrom, account => account.Has(Capability.CanReceiveTransfers)),
        (AccountAction.PayRecurringBill, account => account.Has(Capability.CanPayBills)),
        (AccountAction.Reconcile, account => account.Has(Capability.CanReconcile))
    ];

    public static IReadOnlyList<AccountAction> AllowedFor(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!account.IsActive)
            return [];

        return ActionRules
            .Where(entry => entry.IsAllowed(account))
            .Select(entry => entry.Action)
            .ToList();
    }
}
