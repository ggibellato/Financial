using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class AccountActions
{
    private static readonly IReadOnlyList<(AccountAction Action, Capability Capability)> ActionCapabilities =
    [
        (AccountAction.AddExpense, Capability.HasCategories),
        (AccountAction.AddIncome, Capability.ReceivesIncome),
        (AccountAction.TransferTo, Capability.CanSendTransfers),
        (AccountAction.TransferFrom, Capability.CanReceiveTransfers),
        (AccountAction.PayRecurringBill, Capability.CanPayBills),
        (AccountAction.Reconcile, Capability.CanReconcile)
    ];

    public static IReadOnlyList<AccountAction> AllowedFor(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!account.IsActive)
            return [];

        return ActionCapabilities
            .Where(entry => account.Has(entry.Capability))
            .Select(entry => entry.Action)
            .ToList();
    }
}
