using System.Text;

namespace Financial.Investment.CurrencyBackfill;

public sealed class CurrencyBackfillSummary
{
    public int TransactionsBackfilled { get; private set; }
    public int TransactionsAlreadySet { get; private set; }
    public int CreditsBackfilled { get; private set; }
    public int CreditsAlreadySet { get; private set; }

    private readonly List<string> _unresolvedTransactions = [];
    private readonly List<string> _unresolvedCredits = [];
    private readonly List<string> _unresolvedBrokers = [];
    private readonly List<string> _unaddressableRecords = [];

    public IReadOnlyList<string> UnresolvedTransactions => _unresolvedTransactions;
    public IReadOnlyList<string> UnresolvedCredits => _unresolvedCredits;
    public IReadOnlyList<string> UnresolvedBrokers => _unresolvedBrokers;
    public IReadOnlyList<string> UnaddressableRecords => _unaddressableRecords;

    public void CountTransactionBackfilled() => TransactionsBackfilled++;
    public void CountTransactionAlreadySet() => TransactionsAlreadySet++;
    public void CountCreditBackfilled() => CreditsBackfilled++;
    public void CountCreditAlreadySet() => CreditsAlreadySet++;

    public void FlagUnresolvedTransaction(string description) => _unresolvedTransactions.Add(description);
    public void FlagUnresolvedCredit(string description) => _unresolvedCredits.Add(description);
    public void FlagUnresolvedBroker(string description) => _unresolvedBrokers.Add(description);

    /// <summary>A record with no unique, non-empty Id (only possible on data predating Id-based
    /// editing) can't be safely targeted for an in-place update, so it is left exactly as found
    /// rather than risking updating the wrong record or throwing mid-run.</summary>
    public void FlagUnaddressableRecord(string description) => _unaddressableRecords.Add(description);

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Currency backfill summary");
        builder.AppendLine($"  Transactions: {TransactionsBackfilled} backfilled, {TransactionsAlreadySet} already set");
        builder.AppendLine($"  Credits: {CreditsBackfilled} backfilled, {CreditsAlreadySet} already set");

        AppendSection(builder, "Brokers with an unrecognized currency (skipped):", _unresolvedBrokers);
        AppendSection(builder, "Transactions left with no FX rate snapshot (no rate obtainable):", _unresolvedTransactions);
        AppendSection(builder, "Credits left with no FX rate snapshot (no rate obtainable):", _unresolvedCredits);
        AppendSection(builder, "Records left unmigrated (no unique Id to target safely):", _unaddressableRecords);

        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, string header, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine(header);
        foreach (var item in items)
        {
            builder.AppendLine($"  {item}");
        }
    }
}
