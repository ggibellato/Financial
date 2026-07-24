using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class BankNameResolver
{
    public static bool TryResolve(string? name, IEnumerable<Bank> banks, out Bank? bank)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            bank = null;
            return false;
        }

        bank = banks.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
        return bank is not null;
    }
}
