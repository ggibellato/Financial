using Financial.Investment.CurrencyBackfill;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.InvestmentCurrencyBackfill.Tests;

public class CurrencyBackfillMigratorTests
{
    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-05")]
    public async Task MigrateAsync_TransactionMissingCurrency_BackfillsCurrencyAndSnapshot()
    {
        var (investments, asset) = BuildGraph("XPI", "BRL");
        var transaction = Transaction.Create(new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        asset.AddTransaction(transaction);
        var provider = new StubExchangeRateProvider(0.146m);

        var summary = await CurrencyBackfillMigrator.MigrateAsync(investments, provider, Currency.GBP);

        var migrated = asset.Transactions.Should().ContainSingle().Subject;
        migrated.Currency.Should().Be(Currency.BRL);
        migrated.FxRateSnapshot.Should().NotBeNull();
        migrated.FxRateSnapshot!.Rate.Should().Be(0.146m);
        summary.TransactionsBackfilled.Should().Be(1);
        summary.TransactionsAlreadySet.Should().Be(0);
    }

    [Fact]
    public async Task MigrateAsync_CreditMissingCurrency_BackfillsCurrencyAndSnapshot()
    {
        var (investments, asset) = BuildGraph("XPI", "BRL");
        asset.AddCredit(Credit.Create(new DateTime(2026, 7, 1), Credit.CreditType.Dividend, 10m));
        var provider = new StubExchangeRateProvider(0.146m);

        var summary = await CurrencyBackfillMigrator.MigrateAsync(investments, provider, Currency.GBP);

        var migrated = asset.Credits.Should().ContainSingle().Subject;
        migrated.Currency.Should().Be(Currency.BRL);
        migrated.FxRateSnapshot.Should().NotBeNull();
        summary.CreditsBackfilled.Should().Be(1);
    }

    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-07")]
    public async Task MigrateAsync_TransactionAlreadyBackfilled_IsSkipped()
    {
        var (investments, asset) = BuildGraph("XPI", "BRL");
        var snapshot = FxRateSnapshot.Create(Currency.GBP, 0.146m, FxRateSource.Frankfurter, DateTimeOffset.UtcNow);
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m, currency: Currency.BRL, fxRateSnapshot: snapshot));
        var provider = new StubExchangeRateProvider(0.99m);

        var summary = await CurrencyBackfillMigrator.MigrateAsync(investments, provider, Currency.GBP);

        summary.TransactionsBackfilled.Should().Be(0);
        summary.TransactionsAlreadySet.Should().Be(1);
        provider.CallCount.Should().Be(0, "an already-migrated record must not be reprocessed");
    }

    [Fact]
    public async Task MigrateAsync_BrokerCurrencyMatchesReportingCurrency_NeedsNoSnapshotOnceCurrencyIsSet()
    {
        var (investments, asset) = BuildGraph("Trading212", "GBP");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var provider = new StubExchangeRateProvider(0.146m);

        var summary = await CurrencyBackfillMigrator.MigrateAsync(investments, provider, Currency.GBP);

        var migrated = asset.Transactions.Should().ContainSingle().Subject;
        migrated.Currency.Should().Be(Currency.GBP);
        migrated.FxRateSnapshot.Should().BeNull();
        provider.CallCount.Should().Be(0, "no conversion is needed when the broker already matches the reporting currency");
        summary.TransactionsBackfilled.Should().Be(1);
    }

    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-06")]
    public async Task MigrateAsync_WhenNoRateIsObtainable_LeavesSnapshotNullAndNamesTheRecord()
    {
        var (investments, asset) = BuildGraph("XPI", "BRL");
        var transaction = Transaction.Create(new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        asset.AddTransaction(transaction);
        var provider = new StubExchangeRateProvider(rate: null);

        var summary = await CurrencyBackfillMigrator.MigrateAsync(investments, provider, Currency.GBP);

        var migrated = asset.Transactions.Should().ContainSingle().Subject;
        migrated.Currency.Should().Be(Currency.BRL);
        migrated.FxRateSnapshot.Should().BeNull();
        summary.UnresolvedTransactions.Should().ContainSingle(s => s.Contains(transaction.Id.ToString()));
    }

    [Fact]
    public async Task MigrateAsync_BrokerWithUnrecognizedCurrency_IsFlaggedAndSkipped()
    {
        var (investments, asset) = BuildGraph("Weird", "Pounds");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var provider = new StubExchangeRateProvider(0.146m);

        var summary = await CurrencyBackfillMigrator.MigrateAsync(investments, provider, Currency.GBP);

        summary.UnresolvedBrokers.Should().ContainSingle(s => s.Contains("Weird"));
        summary.TransactionsBackfilled.Should().Be(0);
        summary.TransactionsAlreadySet.Should().Be(0);
    }

    [Fact]
    public async Task MigrateAsync_CreditWithNoId_IsLeftUnmigratedAndFlagged()
    {
        var (investments, asset) = BuildGraph("XPI", "BRL");
        asset.AddCredit(Credit.CreateWithId(Guid.Empty, new DateTime(2026, 7, 1), Credit.CreditType.Dividend, 10m));
        var provider = new StubExchangeRateProvider(0.146m);

        var summary = await CurrencyBackfillMigrator.MigrateAsync(investments, provider, Currency.GBP);

        var untouched = asset.Credits.Should().ContainSingle().Subject;
        untouched.FxRateSnapshot.Should().BeNull();
        summary.CreditsBackfilled.Should().Be(0);
        summary.UnaddressableRecords.Should().ContainSingle();
    }

    private static (Investments Investments, Asset Asset) BuildGraph(string brokerName, string brokerCurrency)
    {
        var investments = Investments.Create();
        var broker = Broker.Create(brokerName, brokerCurrency);
        var portfolio = broker.AddPortfolio("Default");
        var asset = Asset.Create("AAAA", "ISIN", "BVMF", "AAAA");
        portfolio.AddAsset(asset);
        investments.AddActiveBroker(broker);
        return (investments, asset);
    }
}
