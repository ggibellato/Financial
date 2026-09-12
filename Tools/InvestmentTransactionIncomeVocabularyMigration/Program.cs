using Financial.Investment.TransactionIncomeVocabularyMigration;

var dataFilePath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "data-investment.json"));

if (!File.Exists(dataFilePath))
{
    Console.Error.WriteLine($"Data file not found at '{dataFilePath}'.");
    return 1;
}

var summary = TransactionIncomeVocabularyMigrator.Migrate(dataFilePath);

Console.WriteLine($"Transaction income vocabulary migration for '{dataFilePath}'");
Console.WriteLine(new string('=', 60));
Console.WriteLine();
Console.WriteLine(summary);

return 0;
