using Financial.Investment.TransactionIncomeVocabularyMigration;
using FluentAssertions;

namespace Financial.InvestmentTransactionIncomeVocabularyMigration.Tests;

public class TransactionIncomeVocabularyMigratorTests : IDisposable
{
    private readonly string _dataPath = Path.Combine(Path.GetTempPath(), $"transaction-income-vocabulary-migration-{Guid.NewGuid():N}.json");

    private const string SampleData = """
        {
          "ActiveBrokers": [
            {
              "Name": "XPI",
              "Portfolios": [
                {
                  "Name": "Default",
                  "Assets": [
                    {
                      "Ticker": "BOVA11",
                      "Transactions": [],
                      "Credits": [
                        { "Id": "11111111-1111-1111-1111-111111111111", "Date": "2026-01-01T00:00:00", "Type": "Rent", "Value": 1.42 },
                        { "Id": "22222222-2222-2222-2222-222222222222", "Date": "2026-02-01T00:00:00", "Type": "Dividend", "Value": 5.0 }
                      ]
                    }
                  ]
                }
              ]
            }
          ],
          "HistoricBrokers": [
            {
              "Name": "XPI",
              "Portfolios": [
                {
                  "Name": "Uncategorized",
                  "Assets": [
                    {
                      "Ticker": "BBAS3",
                      "Transactions": [],
                      "Credits": [
                        { "Id": "33333333-3333-3333-3333-333333333333", "Date": "2026-03-01T00:00:00", "Type": "Rent", "Value": 0.03 }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

    public void Dispose()
    {
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(_dataPath)}*"))
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Migrate_RewritesEveryRentCreditToSecuritiesLendingIncome()
    {
        File.WriteAllText(_dataPath, SampleData);

        var summary = TransactionIncomeVocabularyMigrator.Migrate(_dataPath);

        summary.RewrittenCount.Should().Be(2);
        var rewritten = File.ReadAllText(_dataPath);
        rewritten.Should().NotContain("\"Type\": \"Rent\"");
        rewritten.Should().Contain("\"Type\": \"SecuritiesLendingIncome\"");
    }

    [Fact]
    public void Migrate_LeavesEveryOtherFieldUnchanged()
    {
        File.WriteAllText(_dataPath, SampleData);

        TransactionIncomeVocabularyMigrator.Migrate(_dataPath);

        var rewritten = File.ReadAllText(_dataPath);
        rewritten.Should().Contain("\"Type\": \"Dividend\"");
        rewritten.Should().Contain("22222222-2222-2222-2222-222222222222");
        rewritten.Should().Contain("\"Value\": 5.0");
        rewritten.Should().Contain("BOVA11");
        rewritten.Should().Contain("BBAS3");
    }

    [Fact]
    public void Migrate_IsIdempotentOnASecondRun()
    {
        File.WriteAllText(_dataPath, SampleData);
        TransactionIncomeVocabularyMigrator.Migrate(_dataPath);

        var secondRun = TransactionIncomeVocabularyMigrator.Migrate(_dataPath);

        secondRun.RewrittenCount.Should().Be(0);
    }

    [Fact]
    public void Migrate_CreatesATimestampedBackupBeforeWriting()
    {
        File.WriteAllText(_dataPath, SampleData);

        TransactionIncomeVocabularyMigrator.Migrate(_dataPath);

        var backups = Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(_dataPath)}.backup-migration-*");
        backups.Should().ContainSingle();
        File.ReadAllText(backups[0]).Should().Be(SampleData, "the backup preserves the pre-migration content exactly");
    }

    [Fact]
    public void Migrate_MissingFile_Throws()
    {
        var act = () => TransactionIncomeVocabularyMigrator.Migrate(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json"));

        act.Should().Throw<FileNotFoundException>();
    }
}
