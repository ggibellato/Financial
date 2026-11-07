using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Rules;

public static class AccountActions
{
    private static readonly IReadOnlyList<(AccountAction Action, Func<Account, bool, bool> IsAllowed)> ActionRules =
    [
        (AccountAction.AddExpense, (account, _) => account.CanPay),
        (AccountAction.AddCharge, (account, _) => account.Has(Capability.HasStatements)),
        (AccountAction.AddRefund, (account, _) => account.Has(Capability.HasStatements)),
        (AccountAction.RefundCreditToBank, (account, isInCredit) => account.Has(Capability.HasStatements) && isInCredit),
        (AccountAction.AddIncome, (account, _) => account.Has(Capability.ReceivesIncome)),
        (AccountAction.TransferTo, (account, _) => account.Has(Capability.CanSendTransfers)),
        (AccountAction.TransferFrom, (account, _) => account.Has(Capability.CanReceiveTransfers)),
        (AccountAction.PayRecurringBill, (account, _) => account.Has(Capability.CanPayBills)),
        (AccountAction.PayCardStatement, (account, _) => account.Has(Capability.CanPayBills)),
        (AccountAction.ReceiveCardCredit, (account, _) => account.Has(Capability.CanReceiveTransfers)),
        (AccountAction.Reconcile, (account, _) => account.Has(Capability.CanReconcile)),
        (AccountAction.Deposit, (account, _) => account.Has(Capability.HasPots)),
        (AccountAction.Withdraw, (account, _) => account.Has(Capability.CanWithdraw)),
        (AccountAction.ManagePots, (account, _) => account.Has(Capability.HasPots)),
        (AccountAction.WithdrawFromPots, (account, _) => account.Has(Capability.CanPayBills)),
        (AccountAction.AddEntry, (account, _) => account.Has(Capability.HasParticipants)),
        (AccountAction.Settle, (account, _) => account.Has(Capability.CanSettle))
    ];

    private const string NothingToSettle = "Nothing to settle.";

    public static IReadOnlyList<AccountAction> AllowedFor(Account account, bool isInCredit = false)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!account.IsActive)
            return [];

        return ActionRules
            .Where(entry => entry.IsAllowed(account, isInCredit))
            .Select(entry => entry.Action)
            .ToList();
    }

    public static IReadOnlyDictionary<AccountAction, string> DisabledFor(
        Account account, IEnumerable<(Currency Currency, decimal Balance)> currentBalances)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(currentBalances);

        var balances = currentBalances.ToList();
        var isSettled = AllowedFor(account).Contains(AccountAction.Settle)
            && balances.Count > 0
            && balances.All(balance => SettlementTransaction.RoundsToZero(balance.Balance));

        return isSettled
            ? new Dictionary<AccountAction, string> { [AccountAction.Settle] = NothingToSettle }
            : new Dictionary<AccountAction, string>();
    }
}
