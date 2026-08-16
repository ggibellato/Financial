namespace Financial.CashFlow.Application.Validation;

public static class EntityLookupExtensions
{
    public static T FirstOrThrow<T>(this IEnumerable<T> items, Func<T, bool> predicate, string entityName, object id) =>
        items.FirstOrDefault(predicate)
            ?? throw new KeyNotFoundException($"{entityName} '{id}' was not found.");
}
