using Financial.Investment.CurrencyBackfill;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.InvestmentCurrencyBackfill.Tests;

/// <summary>
/// Exercises the same load -> migrate -> save round trip Program.cs performs, against a temp JSON
/// file - never the live data-investment.json (repo policy). Every Transaction/Credit here is
/// built via Create() with a real generated Id, matching how the application - and every existing
/// production record - actually populates the data file (Asset.RecordTransaction/AddCredit always
/// go through the same factories).
/// </summary>
public class CurrencyBackfillEndToEndTests
{
    [Fact]
    public async Task MigrateAsync_AgainstATempDataFile_BackfillsAndPersists()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"data.currency-backfill.{Guid.NewGuid():N}.json");
        var serializer = new InvestmentSerializerAdapter();

        try
        {
            var investments = Investments.Create();
            var broker = Broker.Create("XPI", "BRL");
            var portfolio = broker.AddPortfolio("Default");
            var asset = Asset.Create("BCIA11", "ISIN", "BVMF", "BCIA11");
            asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
            asset.AddCredit(Credit.Create(new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 5m));
            portfolio.AddAsset(asset);
            investments.AddActiveBroker(broker);

            File.WriteAllText(tempFile, serializer.Serialize(investments));

            var storage = new LocalJsonStorage(tempFile);
            var loaded = InvestmentLoader.LoadSync(storage, serializer);
            var provider = new StubExchangeRateProvider(0.146m);

            var summary = await CurrencyBackfillMigrator.MigrateAsync(loaded, provider, Currency.GBP);

            var repository = new InvestmentJsonRepository(loaded, storage, serializer);
            await repository.ApplyAndSaveAsync(() => true);

            var reloaded = InvestmentLoader.LoadSync(new LocalJsonStorage(tempFile), serializer);
            var xpi = reloaded.ActiveBrokers.Should().Contain(b => b.Name == "XPI").Subject;
            var reloadedAsset = xpi.Portfolios.Single().Assets.Single();
            reloadedAsset.Transactions.Should().OnlyContain(t => t.Currency == Currency.BRL && t.FxRateSnapshot != null);
            reloadedAsset.Credits.Should().OnlyContain(c => c.Currency == Currency.BRL && c.FxRateSnapshot != null);

            summary.TransactionsBackfilled.Should().Be(1);
            summary.CreditsBackfilled.Should().Be(1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
