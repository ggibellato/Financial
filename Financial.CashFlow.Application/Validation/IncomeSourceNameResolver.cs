using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class IncomeSourceNameResolver
{
    public static bool TryResolve(string? name, IEnumerable<IncomeSource> sources, out IncomeSource? source)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            source = null;
            return false;
        }

        source = sources.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        return source is not null;
    }
}
