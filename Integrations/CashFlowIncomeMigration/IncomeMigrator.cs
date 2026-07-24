using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowIncomeMigration;

/// <summary>
/// Confirms the Incomes collection exists on the data file. There is no fixed data to seed
/// (unlike the P13 bank migration) — CashFlowData.Incomes already default-initializes to an
/// empty list, so this run's only job is to make the run auditable and backed up, per the PRD.
/// </summary>
public static class IncomeMigrator
{
    public static IncomeMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new IncomeMigrationSummary(data.Incomes.Count);
    }
}
