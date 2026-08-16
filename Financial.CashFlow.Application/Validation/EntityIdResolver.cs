namespace Financial.CashFlow.Application.Validation;

public static class EntityIdResolver
{
    public static bool TryResolve<T>(Guid? id, IEnumerable<T> items, Func<T, Guid> idSelector, out T? result)
    {
        if (id is null)
        {
            result = default;
            return false;
        }

        result = items.FirstOrDefault(item => idSelector(item).Equals(id.Value));
        return result is not null;
    }
}
