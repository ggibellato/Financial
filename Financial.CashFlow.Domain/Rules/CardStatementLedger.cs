using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public enum CardStatementLedgerStatus
{
    Unpaid,
    Paid,
    NothingDue
}

public sealed record CardStatementLedgerMonth(
    int Year,
    int Month,
    CardStatement? Statement,
    decimal Total,
    decimal CreditIn,
    decimal AmountToPay,
    decimal CreditCarried,
    decimal? PaidAmount,
    CardStatementLedgerStatus Status)
{
    public bool IsUnpaid => Status == CardStatementLedgerStatus.Unpaid;
}

public static class CardStatementLedger
{
    private const int MonthsPerYear = 12;
    private const int NoThroughMonth = int.MinValue;

    public static IReadOnlyList<CardStatementLedgerMonth> For(
        Account card,
        IEnumerable<Expense> expenses,
        IEnumerable<CardStatement> statements,
        IEnumerable<Transfer> transfers,
        int throughYear,
        int throughMonth) =>
        Walk(card, expenses, statements, transfers, MonthIndex(throughYear, throughMonth));

    public static CardStatementLedgerMonth? MonthOf(
        Account card,
        IEnumerable<Expense> expenses,
        IEnumerable<CardStatement> statements,
        IEnumerable<Transfer> transfers,
        int year,
        int month) =>
        For(card, expenses, statements, transfers, year, month).FirstOrDefault(row => row.Year == year && row.Month == month);

    public static CardStatementLedgerMonth? NextToPay(
        Account card, IEnumerable<Expense> expenses, IEnumerable<CardStatement> statements, IEnumerable<Transfer> transfers) =>
        Walk(card, expenses, statements, transfers, NoThroughMonth).FirstOrDefault(row => row.IsUnpaid);

    public static bool RefundKeepsAmountsToPay(
        Transfer refund, IEnumerable<Expense> expenses, IEnumerable<CardStatement> statements, IEnumerable<Transfer> transfers)
    {
        var throughIndex = MonthIndex(refund.Date.Year, refund.Date.Month);
        var before = Walk(refund.SourceAccount, expenses, statements, transfers, throughIndex);
        var after = Walk(refund.SourceAccount, expenses, statements, transfers.Append(refund), throughIndex);

        var amountsBefore = before.ToDictionary(row => MonthIndex(row.Year, row.Month), row => row.AmountToPay);
        var raisesAnAmount = after.Any(row => row.AmountToPay > amountsBefore.GetValueOrDefault(MonthIndex(row.Year, row.Month)));
        var creditBefore = before.LastOrDefault()?.CreditCarried ?? 0m;

        return !raisesAnAmount && after[^1].CreditCarried == creditBefore - refund.Amount;
    }

    private static List<CardStatementLedgerMonth> Walk(
        Account card, IEnumerable<Expense> expenses, IEnumerable<CardStatement> statements, IEnumerable<Transfer> transfers, int throughIndex)
    {
        var chargesByMonth = expenses
            .Where(expense => expense.CreditCard?.Id == card.Id && expense.InvoiceDate is not null)
            .ToLookup(expense => MonthIndex(expense.InvoiceDate!.Value.Year, expense.InvoiceDate.Value.Month));
        var transferList = transfers.ToList();
        var creditRefundsByMonth = transferList
            .Where(transfer => transfer.SourceAccount.Id == card.Id && transfer.IsCreditRefund)
            .ToLookup(transfer => MonthIndex(transfer.Date.Year, transfer.Date.Month));
        var rows = new List<CardStatementLedgerMonth>();
        var monthsWithRecords = chargesByMonth.Select(group => group.Key).Concat(creditRefundsByMonth.Select(group => group.Key)).ToList();
        if (monthsWithRecords.Count == 0)
            return rows;

        var statementList = statements.ToList();
        var lastIndex = Math.Max(monthsWithRecords.Max(), throughIndex);
        var creditIn = 0m;

        for (var index = monthsWithRecords.Min(); index <= lastIndex; index++)
        {
            var creditRefunded = creditRefundsByMonth[index].Sum(refund => refund.Amount);
            var row = MonthRow(
                card, index / MonthsPerYear, index % MonthsPerYear + 1, statementList, chargesByMonth[index], creditRefunded, transferList, creditIn);
            rows.Add(row);
            creditIn = row.CreditCarried;
        }

        return rows;
    }

    private static CardStatementLedgerMonth MonthRow(
        Account card,
        int year,
        int month,
        List<CardStatement> statements,
        IEnumerable<Expense> charges,
        decimal creditRefunded,
        List<Transfer> transfers,
        decimal creditIn)
    {
        var statement = statements.FirstOrDefault(candidate => candidate.IsFor(card.Id, year, month));
        var total = charges.Sum(charge => charge.Value) + creditRefunded;
        var counted = SumWithStatus(charges, ExpensePaymentStatus.CreditCardCharge) + creditRefunded;

        CardStatementLedgerMonth Row(decimal amountToPay, decimal creditCarried, decimal? paidAmount, CardStatementLedgerStatus status) =>
            new(year, month, statement, total, creditIn, amountToPay, creditCarried, paidAmount, status);

        if (statement is { IsPaid: true, IsPaidByTransfer: false })
            return Row(0m, creditIn, SumWithStatus(charges, ExpensePaymentStatus.CreditCardSettled), CardStatementLedgerStatus.Paid);

        if (statement is { IsPaid: true })
        {
            var paid = transfers.FirstOrDefault(transfer => transfer.Id == statement.PaymentTransferId)?.Amount ?? 0m;
            return Row(0m, Math.Max(0m, creditIn + paid - counted), paid, CardStatementLedgerStatus.Paid);
        }

        var due = counted - creditIn;
        return due > 0m
            ? Row(due, 0m, null, CardStatementLedgerStatus.Unpaid)
            : Row(0m, Math.Abs(due), null, CardStatementLedgerStatus.NothingDue);
    }

    private static decimal SumWithStatus(IEnumerable<Expense> charges, ExpensePaymentStatus status) =>
        charges.Where(charge => charge.PaymentStatus == status).Sum(charge => charge.Value);

    private static int MonthIndex(int year, int month) => year * MonthsPerYear + month - 1;
}
