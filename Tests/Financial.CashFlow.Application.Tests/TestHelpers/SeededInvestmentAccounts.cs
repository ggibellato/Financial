using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Tests.TestHelpers;

/// <summary>
/// The 11 investment accounts a real deployment has after running the
/// CashFlowSpreadsheetImport migration tool once (see InvestmentAccountMigrator),
/// shared by tests that need a fully seeded account list rather than an empty one.
/// </summary>
internal static class SeededInvestmentAccounts
{
    private static readonly (string Name, bool IsLiability)[] Accounts =
    [
        ("BlueRewardsSaver", false),
        ("PlatinumVisa8003", true),
        ("PlatinumVisa6007", true),
        ("ChaseMaster4023", true),
        ("BaAmex", true),
        ("PaypalCredit", true),
        ("ChipCashIsaGleison", false),
        ("ChaseSave", false),
        ("ChipCashIsaAriana", false),
        ("Trading212Invested", false),
        ("ReservasPessoais", true)
    ];

    public static void SeedInto(StubCashFlowRepository repository) =>
        repository.InvestmentAccounts.AddRange(
            Accounts.Select(a => InvestmentAccount.Create(a.Name, isActive: true, isLiability: a.IsLiability)));
}
