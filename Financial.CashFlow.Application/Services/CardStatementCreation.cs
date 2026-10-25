using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

internal static class CardStatementCreation
{
    internal static CardStatement EnsureFor(ICashFlowRepository repository, Account card, int year, int month)
    {
        var existing = repository.GetCardStatements().FirstOrDefault(statement => statement.IsFor(card.Id, year, month));
        if (existing is not null)
        {
            return existing;
        }

        var statement = CardStatement.Create(card, year, month);
        repository.AddCardStatement(statement);
        return statement;
    }
}
