using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class AccountActions
{
    private static readonly IReadOnlyList<(AccountAction Action, Capability Capability, Capability? Excluding)> ActionCapabilities =
    [
        (AccountAction.AddExpense, Capability.HasCategories, Capability.HasStatements),
        (AccountAction.AddIncome, Capability.ReceivesIncome, null),
        (AccountAction.TransferTo, Capability.CanSendTransfers, null),
        (AccountAction.TransferFrom, Capability.CanReceiveTransfers, null),
        (AccountAction.PayRecurringBill, Capability.CanPayBills, null),
        (AccountAction.Reconcile, Capability.CanReconcile, null)
    ];

    public static IReadOnlyList<AccountAction> AllowedFor(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!account.IsActive)
            return [];

        return ActionCapabilities
            .Where(entry => account.Has(entry.Capability) && !(entry.Excluding is { } excluding && account.Has(excluding)))
            .Select(entry => entry.Action)
            .ToList();
    }
}
