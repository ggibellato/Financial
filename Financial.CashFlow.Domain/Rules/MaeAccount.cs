using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class MaeAccount
{
    public const string Name = "Mãe";

    public static Account? Find(IEnumerable<Account> accounts) =>
        accounts.FirstOrDefault(account => account.Has(Capability.HasParticipants));

    public static Account Create()
    {
        var currencies = AccountTemplates.AllowedCurrencies(AccountTemplate.Settlement);
        var mae = Account.Create(Name, AccountTemplate.Settlement, currencies[0]);
        mae.SetCurrencies(currencies);
        mae.SetOtherParticipant(Name);
        return mae;
    }
}
