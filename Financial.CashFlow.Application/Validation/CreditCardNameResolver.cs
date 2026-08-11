using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class CreditCardNameResolver
{
    public static bool TryResolve(Guid? id, IEnumerable<CreditCard> creditCards, out CreditCard? creditCard)
    {
        if (id is null)
        {
            creditCard = null;
            return false;
        }

        creditCard = creditCards.FirstOrDefault(c => c.Id == id.Value);
        return creditCard is not null;
    }
}
