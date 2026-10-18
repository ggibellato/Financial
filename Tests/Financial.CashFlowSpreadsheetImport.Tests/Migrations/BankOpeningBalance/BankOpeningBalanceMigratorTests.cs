using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.BankOpeningBalance;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.BankOpeningBalance;

[Trait("Category", "Unit")]
public class BankOpeningBalanceMigratorTests
{
    private static readonly DateOnly RunDate = new(2026, 7, 24);

    [Fact]
    public void Migrate_BankWithUnsetDate_DefaultsToZeroAndRunDate()
    {
        var data = CashFlowData.Create();
        data.AddAccount(TestAccounts.Bank("Barclays", roundUpEnabled: false));

        var summary = BankOpeningBalanceMigrator.Migrate(data, RunDate);

        summary.BanksDefaultedCount.Should().Be(1);
        summary.BanksAlreadySetCount.Should().Be(0);
        var bank = data.Accounts.Should().ContainSingle().Which;
        bank.OpeningBalance!.Amount.Should().Be(0m);
        bank.OpeningBalance!.StartDate.Should().Be(RunDate);
    }

    [Fact]
    public void Migrate_BankWithAlreadySetDate_IsLeftUntouched()
    {
        var data = CashFlowData.Create();
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(500m, new DateOnly(2026, 1, 1));
        data.AddAccount(bank);

        var summary = BankOpeningBalanceMigrator.Migrate(data, RunDate);

        summary.BanksDefaultedCount.Should().Be(0);
        summary.BanksAlreadySetCount.Should().Be(1);
        bank.OpeningBalance!.Amount.Should().Be(500m);
        bank.OpeningBalance!.StartDate.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void Migrate_CalledTwice_IsIdempotent()
    {
        var data = CashFlowData.Create();
        data.AddAccount(TestAccounts.Bank("Barclays", roundUpEnabled: false));
        BankOpeningBalanceMigrator.Migrate(data, RunDate);

        var secondSummary = BankOpeningBalanceMigrator.Migrate(data, RunDate.AddDays(1));

        secondSummary.BanksDefaultedCount.Should().Be(0);
        secondSummary.BanksAlreadySetCount.Should().Be(1);
        data.Accounts.Should().ContainSingle().Which.OpeningBalance!.StartDate.Should().Be(RunDate);
    }

    [Fact]
    public void Migrate_WithMixOfSetAndUnsetBanks_OnlyDefaultsTheUnsetOnes()
    {
        var data = CashFlowData.Create();
        var alreadySet = TestAccounts.Bank("Chase", roundUpEnabled: true);
        alreadySet.SetOpeningBalance(100m, new DateOnly(2026, 2, 1));
        data.AddAccount(alreadySet);
        data.AddAccount(TestAccounts.Bank("Barclays", roundUpEnabled: false));
        data.AddAccount(TestAccounts.Bank("Trading212", roundUpEnabled: true));

        var summary = BankOpeningBalanceMigrator.Migrate(data, RunDate);

        summary.BanksDefaultedCount.Should().Be(2);
        summary.BanksAlreadySetCount.Should().Be(1);
    }
}
