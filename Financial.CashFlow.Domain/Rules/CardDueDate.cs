using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Domain.Rules;

public static class CardDueDate
{
    public static DateOnly? For(Account card, IEnumerable<CardStatement> statements) =>
        statements
            .Where(statement => statement.CreditCard.Id == card.Id && statement.DueDate is not null)
            .MaxBy(statement => (statement.Year, statement.Month))
            ?.DueDate;

    public static void SetOn(Account card, IEnumerable<CardStatement> statements, CardStatement statement, DateOnly dueDate)
    {
        EnsureWithinMonth(statement.Year, statement.Month, dueDate);

        foreach (var later in statements.Where(candidate => candidate.CreditCard.Id == card.Id && candidate.IsAfter(statement.Year, statement.Month)))
        {
            later.SetDueDate(null);
        }

        statement.SetDueDate(dueDate);
    }

    public static void EnsureDueDateAllowed(CardStatementLedgerMonth month, DateOnly? newDueDate)
    {
        if (month.Status == CardStatementLedgerStatus.Paid)
        {
            throw new ArgumentException($"The {MonthLabel(month.Year, month.Month)} statement is paid, so its due date can't change.");
        }

        if (newDueDate is { } date)
        {
            EnsureWithinMonth(month.Year, month.Month, date);
        }
    }

    private static void EnsureWithinMonth(int year, int month, DateOnly dueDate)
    {
        if (dueDate.Year != year || dueDate.Month != month)
        {
            throw new ArgumentException($"The due date must be in {MonthLabel(year, month)}.");
        }
    }

    private static string MonthLabel(int year, int month) =>
        new DateOnly(year, month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
}
