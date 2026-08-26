using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;

public sealed class InvestmentAccountMigrationSummary : MigrationSummaryBase
{
    private readonly List<InvestmentSnapshot> _unresolvedSnapshots = new();

    public int AccountsSeededCount => SeededCount;
    public int AccountsAlreadyPresentCount => AlreadyPresentCount;
    public int SnapshotsResolvedCount { get; private set; }

    public IReadOnlyList<InvestmentSnapshot> UnresolvedSnapshots => _unresolvedSnapshots;

    public void CountAccountSeeded() => CountSeeded();
    public void CountAccountAlreadyPresent() => CountAlreadyPresent();
    public void CountSnapshotResolved() => SnapshotsResolvedCount++;

    public void FlagUnresolvedSnapshot(InvestmentSnapshot snapshot) => _unresolvedSnapshots.Add(snapshot);

    public string Render()
    {
        var builder = new StringBuilder();
        AppendHeader(builder, "Investment account", "Accounts");
        builder.AppendLine($"  Snapshots: {SnapshotsResolvedCount} resolved");

        AppendUnresolvedSection(builder,
            "Snapshots whose account does not match any seeded account (review manually):",
            _unresolvedSnapshots, snapshot => $"{snapshot.Id} {snapshot.Year}-{snapshot.Month:D2} [{snapshot.Account.Name}]");

        return builder.ToString();
    }
}
