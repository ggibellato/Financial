namespace Financial.CashFlow.Domain.Enums;

public enum AccountAction
{
    AddExpense,
    AddCharge,
    AddRefund,
    RefundCreditToBank,
    AddIncome,
    TransferTo,
    TransferFrom,
    PayRecurringBill,
    PayCardStatement,
    Reconcile,
    Deposit,
    Withdraw,
    ManagePots,
    WithdrawFromPots
}
