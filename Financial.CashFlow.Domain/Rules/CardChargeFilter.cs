using System;
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

    public static IEnumerable<Expense> SettledChargesFor(IEnumerable<Expense> expenses, Account card, int year, int month) =>
        ChargesFor(expenses, card, year, month).Where(expense => expense.PaymentStatus == ExpensePaymentStatus.CreditCardSettled);

    public static bool IsPaid(Expense expense, IEnumerable<CardStatement> statements) =>
        expense.PaymentStatus == ExpensePaymentStatus.CreditCardSettled || IsOnPaidStatement(expense, statements);

    public static bool IsOnPaidStatement(Expense expense, IEnumerable<CardStatement> statements) =>
        expense is { CreditCard: { } card, InvoiceDate: { } invoiceDate }
        && IsStatementPaid(statements, card, invoiceDate.Year, invoiceDate.Month);

    public static bool IsPaidThroughStatement(Expense expense, IEnumerable<CardStatement> statements) =>
        expense.PaymentStatus == ExpensePaymentStatus.CreditCardCharge && IsOnPaidStatement(expense, statements);

    public static decimal TotalOwed(Account card, IEnumerable<IAccountTransaction> records) =>
        -AccountBalanceRule.Balance(card, DateOnly.MaxValue, records);

    public static bool IsStatementPaid(IEnumerable<CardStatement> statements, Account card, int year, int month) =>
        statements.Any(statement => statement.IsPaid && statement.IsFor(card.Id, year, month));
}
