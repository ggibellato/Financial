namespace Financial.CashFlow.Application.DTOs;

public sealed class RecurringBillMarkPaidDTO
{
    /// <summary>Bank expense to record together with the Paid status. Omit to mark the bill Paid without an expense.</summary>
    public RecurringBillPaymentExpenseDTO? Expense { get; init; }
}
