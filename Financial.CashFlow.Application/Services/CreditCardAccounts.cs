using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Application.Services;

internal static class CreditCardAccounts
{
    internal static IEnumerable<Account> GetCreditCardAccounts(this ICashFlowRepository repository) =>
        repository.GetAccounts().Where(account => account.Has(Capability.HasStatements));

    internal static DateOnly? GetCardDueDate(this ICashFlowRepository repository, Account card) =>
        CardDueDate.For(card, repository.GetCardStatements());
}
