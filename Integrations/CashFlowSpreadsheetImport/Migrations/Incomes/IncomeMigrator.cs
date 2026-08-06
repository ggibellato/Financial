using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Incomes;

/// <summary>
/// Confirms the Incomes collection exists on the data file. CashFlowData.Incomes already
/// default-initializes to an empty list, so the run's only job is to make it auditable and
/// backed up.
/// </summary>
public static class IncomeMigrator
{
    public static IncomeMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new IncomeMigrationSummary(data.Incomes.Count);
    }
}
