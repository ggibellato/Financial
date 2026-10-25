using System;
using System.Collections.Generic;
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

    public static void ClearSupersededBy(Account card, IEnumerable<CardStatement> statements, DateOnly? newDueDate)
    {
        var superseded = statements
            .Where(statement => statement.CreditCard.Id == card.Id
                && (newDueDate is not { } date || statement.IsAfter(date.Year, date.Month)))
            .ToList();

        foreach (var statement in superseded)
        {
            statement.SetDueDate(null);
        }
    }
}
