using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;

/// <summary>
/// Outcome of one migration run: how many investment accounts were seeded and how every
/// existing snapshot's account reference audited against them, plus anything that needs
/// manual review.
/// </summary>
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
                builder.AppendLine($"  {snapshot.Id} {snapshot.Year}-{snapshot.Month:D2} [{snapshot.Account}]");
            }
        }

        return builder.ToString();
    }
}
