using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowBankOpeningBalanceMigration;
using FluentAssertions;

namespace Financial.CashFlowBankOpeningBalanceMigration.Tests;

public class BankOpeningBalanceMigratorTests
{
    private static readonly DateOnly RunDate = new(2026, 7, 24);

    [Fact]
    public void Migrate_BankWithUnsetDate_DefaultsToZeroAndRunDate()
    {
        var data = CashFlowData.Create();
        data.AddBank(Bank.Create("Barclays", roundUpEnabled: false));

        var summary = BankOpeningBalanceMigrator.Migrate(data, RunDate);

        summary.BanksDefaultedCount.Should().Be(1);
        summary.BanksAlreadySetCount.Should().Be(0);
        var bank = data.Banks.Should().ContainSingle().Which;
        bank.OpeningBalance.Should().Be(0m);
        bank.OpeningBalanceDate.Should().Be(RunDate);
    }

    [Fact]
    public void Migrate_BankWithAlreadySetDate_IsLeftUntouched()
    {
        var data = CashFlowData.Create();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(500m, new DateOnly(2026, 1, 1));
        data.AddBank(bank);

        var summary = BankOpeningBalanceMigrator.Migrate(data, RunDate);

        summary.BanksDefaultedCount.Should().Be(0);
        summary.BanksAlreadySetCount.Should().Be(1);
        bank.OpeningBalance.Should().Be(500m);
        bank.OpeningBalanceDate.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void Migrate_CalledTwice_IsIdempotent()
    {
        var data = CashFlowData.Create();
        data.AddBank(Bank.Create("Barclays", roundUpEnabled: false));
        BankOpeningBalanceMigrator.Migrate(data, RunDate);

        var secondSummary = BankOpeningBalanceMigrator.Migrate(data, RunDate.AddDays(1));

        secondSummary.BanksDefaultedCount.Should().Be(0);
        secondSummary.BanksAlreadySetCount.Should().Be(1);
        data.Banks.Should().ContainSingle().Which.OpeningBalanceDate.Should().Be(RunDate);
    }

    [Fact]
    public void Migrate_WithMixOfSetAndUnsetBanks_OnlyDefaultsTheUnsetOnes()
    {
        var data = CashFlowData.Create();
        var alreadySet = Bank.Create("Chase", roundUpEnabled: true);
        alreadySet.SetOpeningBalance(100m, new DateOnly(2026, 2, 1));
        data.AddBank(alreadySet);
        data.AddBank(Bank.Create("Barclays", roundUpEnabled: false));
        data.AddBank(Bank.Create("Trading212", roundUpEnabled: true));

        var summary = BankOpeningBalanceMigrator.Migrate(data, RunDate);

        summary.BanksDefaultedCount.Should().Be(2);
        summary.BanksAlreadySetCount.Should().Be(1);
    }

    [Fact]
    public void Migrate_WithNullData_Throws()
    {
        var act = () => BankOpeningBalanceMigrator.Migrate(null!, RunDate);

        act.Should().Throw<ArgumentNullException>();
    }
}
