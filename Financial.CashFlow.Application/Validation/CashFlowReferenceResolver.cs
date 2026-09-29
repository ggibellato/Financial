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

    public static Bank ResolvePaymentSource(IEnumerable<Bank> banks, Guid bankId)
    {
        if (!EntityIdResolver.TryResolve(bankId, banks, b => b.Id, out var bank))
        {
            throw new ArgumentException($"Payment source '{bankId}' is not recognized.");
        }

        return bank;
    }
}
