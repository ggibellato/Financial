using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class CashFlowReferenceResolver
{
    public static Category ResolveActiveCategory(IEnumerable<Category> categories, Guid categoryId)
    {
        if (!EntityIdResolver.TryResolve(categoryId, categories, c => c.Id, out var category))
        {
            throw new ArgumentException($"Category '{categoryId}' is not recognized.");
        }

        if (!category.Active)
        {
            throw new ArgumentException($"Category '{category.Name}' is inactive and cannot be used for new entries.");
        }

        return category;
    }

    public static Account ResolvePaymentSource(IEnumerable<Account> accounts, Guid accountId)
    {
        if (!EntityIdResolver.TryResolve(accountId, accounts, a => a.Id, out var account))
        {
            throw new ArgumentException($"Payment source '{accountId}' is not recognized.");
        }

        return account;
    }

    public static Account? ResolvePotsAccount(IEnumerable<Account> accounts, Guid? potsAccountId)
    {
        if (potsAccountId is null)
        {
            return null;
        }

        if (!EntityIdResolver.TryResolve(potsAccountId, accounts, a => a.Id, out var potsAccount))
        {
            throw new ArgumentException($"Pots account '{potsAccountId}' is not recognized.");
        }

        return potsAccount;
    }

    public static Account ResolveAccount(IEnumerable<Account> accounts, Guid accountId) =>
        EntityIdResolver.TryResolve(accountId, accounts, a => a.Id, out var account)
            ? account
            : throw new ArgumentException($"Account '{accountId}' was not found.");
}
