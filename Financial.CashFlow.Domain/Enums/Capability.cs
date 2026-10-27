namespace Financial.CashFlow.Domain.Enums;

public enum Capability
{
    HasOpeningBalance,
    ReceivesIncome,
    HasCategories,
    CanSendTransfers,
    CanReceiveTransfers,
    CanReconcile,
    CanPayBills,
    SupportsRoundUp,
    HasStatements,
    ReceivesPayments,
    HasPots,
    ReceivesIncomeSplit,
    CanWithdraw
}
