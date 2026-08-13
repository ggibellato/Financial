using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class BankServiceTests
{
    private static IncomeSource Gleison => IncomeSource.Create("Gleison", IncomeGroup.Salary);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new BankService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetBanks_MapsEveryRepositoryBankToADto()
    {
        var repository = new StubCashFlowRepository();
        repository.Banks.Add(Bank.Create("Barclays", roundUpEnabled: false));
        repository.Banks.Add(Bank.Create("Trading212", roundUpEnabled: true));
        var service = new BankService(repository);

        var result = service.GetBanks();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            result.Should().ContainSingle(b => b.Name == "Barclays" && !b.RoundUpEnabled);
            result.Should().ContainSingle(b => b.Name == "Trading212" && b.RoundUpEnabled);
        }
    }

    [Fact]
    public void GetBanks_WithNoBanks_ReturnsEmptyList()
    {
        var service = new BankService(new StubCashFlowRepository());

        var result = service.GetBanks();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_WithValidRequest_UpdatesAndSaves()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        repository.Banks.Add(bank);
        var service = new BankService(repository);
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = 1250.75m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var result = await service.UpdateOpeningBalanceAsync(bank.Id, request);

        using (new AssertionScope())
        {
            result.OpeningBalance.Should().Be(1250.75m);
            result.OpeningBalanceDate.Should().Be(new DateOnly(2026, 7, 1));
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new BankService(new StubCashFlowRepository());
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = 10m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var act = async () => await service.UpdateOpeningBalanceAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_WithNegativeBalance_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        repository.Banks.Add(bank);
        var service = new BankService(repository);
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = -1m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var act = async () => await service.UpdateOpeningBalanceAsync(bank.Id, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetBankBalancesByMonth_CombinesOpeningBalanceIncomeAndExpenses()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Gleison, null, 500m, bank));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), bank, null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 550m);
    }

    [Fact]
    public void GetBankBalancesByMonth_AddsRoundUpAmountToExpenseValue()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Trading212", roundUpEnabled: true);
        bank.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        var expense = Expense.Create(new DateOnly(2026, 7, 5), "TfL", 9.40m, Category.Create("Extras"), bank, null);
        expense.SetRoundUpAmount(0.60m);
        repository.Expenses.Add(expense);
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Trading212" && b.Balance == -10.00m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesActivityBeforeOpeningBalanceDate()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        repository.Banks.Add(bank);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 6, 30), Gleison, null, 500m, bank));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 6, 30), "Groceries", 50m, Category.Create("Mercado"), bank, null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesActivityAfterSelectedMonth()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 8, 1), Gleison, null, 500m, bank));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_WithNoActivity_ReturnsOpeningBalance()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(250m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 250m);
    }

    [Fact]
    public void GetBankBalancesByMonth_IgnoresActivityTaggedToADifferentBank()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        var chase = Bank.Create("Chase", roundUpEnabled: false);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Gleison, null, 500m, chase));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), chase, null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 0m);
    }

    [Fact]
    public void GetBankBalancesByMonth_AddsTransferAmountToDestinationBank()
    {
        var repository = new StubCashFlowRepository();
        var barclays = Bank.Create("Barclays", roundUpEnabled: false);
        barclays.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        trading212.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(barclays);
        repository.Banks.Add(trading212);
        repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 5), barclays, trading212, 500m, null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Trading212" && b.Balance == 500m);
    }

    [Fact]
    public void GetBankBalancesByMonth_SubtractsTransferAmountFromSourceBank()
    {
        var repository = new StubCashFlowRepository();
        var barclays = Bank.Create("Barclays", roundUpEnabled: false);
        barclays.SetOpeningBalance(1000m, new DateOnly(2026, 1, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        trading212.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(barclays);
        repository.Banks.Add(trading212);
        repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 5), barclays, trading212, 500m, null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 500m);
    }

    [Fact]
    public void GetBankBalancesByMonth_IgnoresTransferTouchingNeitherRoleForTheBank()
    {
        var repository = new StubCashFlowRepository();
        var barclays = Bank.Create("Barclays", roundUpEnabled: false);
        barclays.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        var chase = Bank.Create("Chase", roundUpEnabled: false);
        repository.Banks.Add(barclays);
        repository.Banks.Add(trading212);
        repository.Banks.Add(chase);
        repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 5), trading212, chase, 500m, null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_AddsBalanceAdjustmentDelta()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 7, 5), bank, 150m, 50m, null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 150m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesTransferAndAdjustmentAfterTheAsOfDate()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        repository.Banks.Add(bank);
        repository.Banks.Add(trading212);
        repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 8, 1), bank, trading212, 500m, null));
        repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 8, 1), bank, 999m, 899m, null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesTransferAndAdjustmentBeforeOpeningBalanceDate()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        repository.Banks.Add(bank);
        repository.Banks.Add(trading212);
        repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 6, 30), bank, trading212, 500m, null));
        repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 6, 30), bank, 999m, 899m, null));
        var service = new BankService(repository);

        var result = service.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalanceAsOf_ComputesBalanceForAnArbitraryDateIndependentOfMonthBoundaries()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 10), Gleison, null, 200m, bank));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 20), Gleison, null, 300m, bank));
        var service = new BankService(repository);

        var result = service.GetBankBalanceAsOf(bank.Id, new DateOnly(2026, 7, 15));

        result.Should().Be(300m);
    }

    [Fact]
    public void GetBankBalanceAsOf_WithExcludingAdjustmentId_OmitsOnlyThatAdjustment()
    {
        var repository = new StubCashFlowRepository();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        repository.Banks.Add(bank);
        var chase = Bank.Create("Chase", roundUpEnabled: false);
        repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 1), bank, chase, 20m, null));
        var excluded = BalanceAdjustment.Create(new DateOnly(2026, 7, 1), bank, 500m, 400m, null);
        var included = BalanceAdjustment.Create(new DateOnly(2026, 7, 2), bank, 50m, -30m, null);
        repository.BalanceAdjustments.Add(excluded);
        repository.BalanceAdjustments.Add(included);
        var service = new BankService(repository);

        var result = service.GetBankBalanceAsOf(bank.Id, new DateOnly(2026, 7, 15), excludingAdjustmentId: excluded.Id);

        // 100 (opening) - 20 (transfer out) - 30 (included adjustment delta) = 50; excluded adjustment's +400 is omitted
        result.Should().Be(50m);
    }

    [Fact]
    public void GetBankBalanceAsOf_WithUnresolvableBank_ThrowsKeyNotFoundException()
    {
        var service = new BankService(new StubCashFlowRepository());

        var act = () => service.GetBankBalanceAsOf(Guid.NewGuid(), new DateOnly(2026, 7, 15));

        act.Should().Throw<KeyNotFoundException>();
    }
}
