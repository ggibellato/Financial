using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class IncomeSourceNameResolver
{
    public static bool TryResolve(Guid? id, IEnumerable<IncomeSource> sources, out IncomeSource? source)
    {
        if (id is null)
        {
            source = null;
            return false;
        }

        source = sources.FirstOrDefault(s => s.Id == id.Value);
        return source is not null;
    }
}
