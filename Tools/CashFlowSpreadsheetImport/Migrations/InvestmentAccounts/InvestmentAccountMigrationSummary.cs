using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;

public sealed class InvestmentAccountMigrationSummary
{
    private readonly List<InvestmentSnapshot> _unresolvedSnapshots = new();

    public int AccountsSeededCount { get; private set; }
    public int AccountsAlreadyPresentCount { get; private set; }
    public int SnapshotsResolvedCount { get; private set; }

    public IReadOnlyList<InvestmentSnapshot> UnresolvedSnapshots => _unresolvedSnapshots;

    public void CountAccountSeeded() => AccountsSeededCount++;
    public void CountAccountAlreadyPresent() => AccountsAlreadyPresentCount++;
    public void CountSnapshotResolved() => SnapshotsResolvedCount++;

    public void FlagUnresolvedSnapshot(InvestmentSnapshot snapshot) => _unresolvedSnapshots.Add(snapshot);

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Investment account migration summary");
        builder.AppendLine($"  Accounts: {AccountsSeededCount} seeded, {AccountsAlreadyPresentCount} already present");
        builder.AppendLine($"  Snapshots: {SnapshotsResolvedCount} resolved");

        if (_unresolvedSnapshots.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Snapshots whose account does not match any seeded account (review manually):");
            foreach (var snapshot in _unresolvedSnapshots)
            {
                builder.AppendLine($"  {snapshot.Id} {snapshot.Year}-{snapshot.Month:D2} [{snapshot.Account.Name}]");
            }
        }

        return builder.ToString();
    }
}
