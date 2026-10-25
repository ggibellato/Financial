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
}
