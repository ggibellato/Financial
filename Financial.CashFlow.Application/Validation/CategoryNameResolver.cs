using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class CategoryNameResolver
{
    public static bool TryResolve(Guid? id, IEnumerable<Category> categories, out Category? category)
    {
        if (id is null)
        {
            category = null;
            return false;
        }

        category = categories.FirstOrDefault(c => c.Id == id.Value);
        return category is not null;
    }
}
