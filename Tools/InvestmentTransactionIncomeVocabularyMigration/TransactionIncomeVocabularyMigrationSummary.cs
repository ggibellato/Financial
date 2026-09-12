namespace Financial.Investment.TransactionIncomeVocabularyMigration;

public sealed class TransactionIncomeVocabularyMigrationSummary
{
    public int RewrittenCount { get; private set; }

    internal void CountRewritten() => RewrittenCount++;

    public override string ToString() =>
        $"Transaction income vocabulary migration summary{Environment.NewLine}  Rent -> SecuritiesLendingIncome: {RewrittenCount} credit(s) rewritten";
}
