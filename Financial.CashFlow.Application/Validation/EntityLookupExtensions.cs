using Financial.CashFlow.Application.Exceptions;

namespace Financial.CashFlow.Application.Validation;

public static class EntityLookupExtensions
{
    public static T FirstOrThrow<T>(this IEnumerable<T> items, Func<T, bool> predicate, string entityName, object id) =>
        items.FirstOrDefault(predicate)
            ?? throw new KeyNotFoundException($"{entityName} '{id}' was not found.");

    /// <param name="entityDisplayName">The entity noun with its article, e.g. "A bank" or "An income source".</param>
    public static void EnsureNameIsUnique<T>(
        this IEnumerable<T> items,
        string name,
        Guid? excludingId,
        Func<T, string> nameSelector,
        Func<T, Guid> idSelector,
        string entityDisplayName)
    {
        var collision = items.FirstOrDefault(item => nameSelector(item) == name && idSelector(item) != excludingId);
        if (collision is not null)
        {
            throw new DuplicateNameException($"{entityDisplayName} named \"{name}\" already exists.");
        }
    }
}
