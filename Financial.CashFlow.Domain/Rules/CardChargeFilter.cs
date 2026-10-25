using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class CardChargeFilter
{
    public static IEnumerable<Expense> ChargesFor(IEnumerable<Expense> expenses, Account card, int year, int month, bool includePaid) =>
        expenses.Where(expense => expense.CreditCard?.Id == card.Id
            && expense.InvoiceDate is { } invoiceDate
            && invoiceDate.Year == year
            && invoiceDate.Month == month
            && (includePaid || expense.PaymentStatus == ExpensePaymentStatus.CreditCardCharge));
}
