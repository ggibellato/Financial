using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Application.Services;

internal static class CardStatementCreation
{
    internal static void EnsureFor(ICashFlowRepository repository, Expense expense)
    {
        if (expense is { CreditCard: { } card, InvoiceDate: { } invoiceDate })
        {
            EnsureFor(repository, card, invoiceDate.Year, invoiceDate.Month);
        }
    }

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
