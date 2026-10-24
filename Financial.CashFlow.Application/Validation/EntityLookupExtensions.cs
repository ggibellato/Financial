using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Domain.Entities;

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
        string entityDisplayName,
        IEqualityComparer<string>? comparer = null)
    {
        comparer ??= StringComparer.Ordinal;
        var collision = items.FirstOrDefault(item => comparer.Equals(nameSelector(item), name) && idSelector(item) != excludingId);
        if (collision is not null)
        {
            throw new DuplicateNameException($"{entityDisplayName} named \"{name}\" already exists.");
        }
    }

    public static void EnsureAccountNameIsUnique(this IEnumerable<Account> accounts, string? name, Guid? excludingId, string entityDisplayName) =>
        accounts.EnsureNameIsUnique(
            name?.Trim() ?? string.Empty, excludingId, a => a.Name, a => a.Id, entityDisplayName, StringComparer.OrdinalIgnoreCase);
}
