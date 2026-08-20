using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class BankServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<BankService> Logger = NullLogger<BankService>.Instance;

    private static IncomeSource Gleison => IncomeSource.Create("Gleison", IncomeGroup.Salary);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly BankService _sut;

    public BankServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository does not repeat the whole construction sequence.</summary>
    private BankService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new BankService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new BankService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetBanks_MapsEveryRepositoryBankToADto()
    {
        _repository.Banks.Add(Bank.Create("Barclays", roundUpEnabled: false));
        _repository.Banks.Add(Bank.Create("Trading212", roundUpEnabled: true));

        var result = _sut.GetBanks();

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
        var result = _sut.GetBanks();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_WithValidRequest_UpdatesAndSaves()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Banks.Add(bank);
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = 1250.75m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var result = await _sut.UpdateOpeningBalanceAsync(bank.Id, request);

        using (new AssertionScope())
        {
            result.OpeningBalance.Should().Be(1250.75m);
            result.OpeningBalanceDate.Should().Be(new DateOnly(2026, 7, 1));
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = 10m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var act = async () => await _sut.UpdateOpeningBalanceAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateOpeningBalanceAsync_WithNegativeBalance_ThrowsArgumentException()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Banks.Add(bank);
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = -1m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var act = async () => await _sut.UpdateOpeningBalanceAsync(bank.Id, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetBankBalancesByMonth_CombinesOpeningBalanceIncomeAndExpenses()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Gleison, null, 500m, bank));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), bank, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 550m);
    }

    [Fact]
    public void GetBankBalancesByMonth_AddsRoundUpAmountToExpenseValue()
    {
        var bank = Bank.Create("Trading212", roundUpEnabled: true);
        bank.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(bank);
        var expense = Expense.Create(new DateOnly(2026, 7, 5), "TfL", 9.40m, Category.Create("Extras"), bank, null);
        expense.SetRoundUpAmount(0.60m);
        _repository.Expenses.Add(expense);

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Trading212" && b.Balance == -10.00m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesActivityBeforeOpeningBalanceDate()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        _repository.Banks.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 6, 30), Gleison, null, 500m, bank));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 6, 30), "Groceries", 50m, Category.Create("Mercado"), bank, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesActivityAfterSelectedMonth()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 8, 1), Gleison, null, 500m, bank));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_WithNoActivity_ReturnsOpeningBalance()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(250m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(bank);

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 250m);
    }

    [Fact]
    public void GetBankBalancesByMonth_IgnoresActivityTaggedToADifferentBank()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(bank);
        var chase = Bank.Create("Chase", roundUpEnabled: false);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Gleison, null, 500m, chase));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), chase, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 0m);
    }

    [Fact]
    public void GetBankBalancesByMonth_AddsTransferAmountToDestinationBank()
    {
        var barclays = Bank.Create("Barclays", roundUpEnabled: false);
        barclays.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        trading212.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(barclays);
        _repository.Banks.Add(trading212);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 5), barclays, trading212, 500m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Trading212" && b.Balance == 500m);
    }

    [Fact]
    public void GetBankBalancesByMonth_SubtractsTransferAmountFromSourceBank()
    {
        var barclays = Bank.Create("Barclays", roundUpEnabled: false);
        barclays.SetOpeningBalance(1000m, new DateOnly(2026, 1, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        trading212.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(barclays);
        _repository.Banks.Add(trading212);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 5), barclays, trading212, 500m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 500m);
    }

    [Fact]
    public void GetBankBalancesByMonth_IgnoresTransferTouchingNeitherRoleForTheBank()
    {
        var barclays = Bank.Create("Barclays", roundUpEnabled: false);
        barclays.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        var chase = Bank.Create("Chase", roundUpEnabled: false);
        _repository.Banks.Add(barclays);
        _repository.Banks.Add(trading212);
        _repository.Banks.Add(chase);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 5), trading212, chase, 500m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_AddsBalanceAdjustmentDelta()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(bank);
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 7, 5), bank, 150m, 50m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 150m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesTransferAndAdjustmentAfterTheAsOfDate()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        _repository.Banks.Add(bank);
        _repository.Banks.Add(trading212);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 8, 1), bank, trading212, 500m, null));
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 8, 1), bank, 999m, 899m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesTransferAndAdjustmentBeforeOpeningBalanceDate()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        var trading212 = Bank.Create("Trading212", roundUpEnabled: false);
        _repository.Banks.Add(bank);
        _repository.Banks.Add(trading212);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 6, 30), bank, trading212, 500m, null));
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 6, 30), bank, 999m, 899m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalanceAsOf_ComputesBalanceForAnArbitraryDateIndependentOfMonthBoundaries()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 10), Gleison, null, 200m, bank));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 20), Gleison, null, 300m, bank));

        var result = _sut.GetBankBalanceAsOf(bank.Id, new DateOnly(2026, 7, 15));

        result.Should().Be(300m);
    }

    [Fact]
    public void GetBankBalanceAsOf_WithExcludingAdjustmentId_OmitsOnlyThatAdjustment()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(bank);
        var chase = Bank.Create("Chase", roundUpEnabled: false);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 1), bank, chase, 20m, null));
        var excluded = BalanceAdjustment.Create(new DateOnly(2026, 7, 1), bank, 500m, 400m, null);
        var included = BalanceAdjustment.Create(new DateOnly(2026, 7, 2), bank, 50m, -30m, null);
        _repository.BalanceAdjustments.Add(excluded);
        _repository.BalanceAdjustments.Add(included);

        var result = _sut.GetBankBalanceAsOf(bank.Id, new DateOnly(2026, 7, 15), excludingAdjustmentId: excluded.Id);

        // 100 (opening) - 20 (transfer out) - 30 (included adjustment delta) = 50; excluded adjustment's +400 is omitted
        result.Should().Be(50m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesBankLessIncome()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Banks.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Gleison, null, 500m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalanceAsOf_WithUnresolvableBank_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.GetBankBalanceAsOf(Guid.NewGuid(), new DateOnly(2026, 7, 15));

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new BankService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
