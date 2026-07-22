using System.Collections.Generic;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class InvestmentAccountClassification
{
    private static readonly HashSet<InvestmentAccount> LiabilityAccounts =
    [
        InvestmentAccount.PlatinumVisa8003,
        InvestmentAccount.PlatinumVisa6007,
        InvestmentAccount.ChaseMaster4023,
        InvestmentAccount.BaAmex,
        InvestmentAccount.PaypalCredit
    ];

    public static bool IsLiability(InvestmentAccount account) => LiabilityAccounts.Contains(account);
}
