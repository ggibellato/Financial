using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowBankOpeningBalanceMigration;

/// <summary>
/// Idempotently defaults every bank's opening balance to £0.00 as of the migration run date.
/// A bank whose OpeningBalanceDate is still the CLR default (0001-01-01) has never been
/// migrated or manually set, since no real bank balance is ever dated year 1; any other
/// date — including one set by a prior run or a manual correction — is left untouched.
/// </summary>
public static class BankOpeningBalanceMigrator
{
    public static BankOpeningBalanceMigrationSummary Migrate(CashFlowData data, DateOnly runDate)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new BankOpeningBalanceMigrationSummary();

        foreach (var bank in data.Banks)
        {
            if (bank.OpeningBalanceDate == default)
            {
                bank.SetOpeningBalance(0m, runDate);
                summary.CountBankDefaulted();
            }
            else
            {
                summary.CountBankAlreadySet();
            }
        }

        return summary;
    }
}
