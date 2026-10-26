using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class CardChargeFilter
{
    public static IEnumerable<Expense> ChargesFor(IEnumerable<Expense> expenses, Account card, int year, int month) =>
        expenses.Where(expense => expense.CreditCard?.Id == card.Id
            && expense.InvoiceDate is { } invoiceDate
            && invoiceDate.Year == year
            && invoiceDate.Month == month);

    public static IEnumerable<Expense> UnpaidChargesFor(
        IEnumerable<Expense> expenses, IEnumerable<CardStatement> statements, Account card, int year, int month) =>
        IsStatementPaid(statements, card, year, month)
            ? []
            : ChargesFor(expenses, card, year, month).Where(expense => expense.PaymentStatus == ExpensePaymentStatus.CreditCardCharge);

    public static decimal OutstandingTotal(
        IEnumerable<Expense> expenses, IEnumerable<CardStatement> statements, Account card, int year, int month) =>
        UnpaidChargesFor(expenses, statements, card, year, month).Sum(expense => expense.Value);

    public static bool IsPaid(Expense expense, IEnumerable<CardStatement> statements) =>
        expense.PaymentStatus == ExpensePaymentStatus.CreditCardSettled || IsOnPaidStatement(expense, statements);

    public static bool IsOnPaidStatement(Expense expense, IEnumerable<CardStatement> statements) =>
        expense is { CreditCard: { } card, InvoiceDate: { } invoiceDate }
        && IsStatementPaid(statements, card, invoiceDate.Year, invoiceDate.Month);

    public static bool IsStatementPaid(IEnumerable<CardStatement> statements, Account card, int year, int month) =>
        statements.Any(statement => statement.IsPaid && statement.IsFor(card.Id, year, month));
}
