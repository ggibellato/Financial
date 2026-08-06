using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class BankNameResolver
{
    public static bool TryResolve(Guid? id, IEnumerable<Bank> banks, out Bank? bank)
    {
        if (id is null)
        {
            bank = null;
            return false;
        }

        bank = banks.FirstOrDefault(b => b.Id == id.Value);
        return bank is not null;
    }
}
