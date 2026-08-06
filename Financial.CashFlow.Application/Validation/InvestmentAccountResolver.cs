using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class InvestmentAccountResolver
{
    public static bool TryResolve(Guid? id, IEnumerable<InvestmentAccount> accounts, out InvestmentAccount? account)
    {
        if (id is null)
        {
            account = null;
            return false;
        }

        account = accounts.FirstOrDefault(a => a.Id == id.Value);
        return account is not null;
    }
}
