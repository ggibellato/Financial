using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
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
    public async Task CreateBankAsync_WithValidRequest_AddsAndSaves()
    {
        var request = new BankCreateDTO { Name = "Barclays", RoundUpEnabled = true };

        var result = await _sut.CreateBankAsync(request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Barclays");
            result.RoundUpEnabled.Should().BeTrue();
            _repository.Banks.Should().ContainSingle(b => b.Name == "Barclays");
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateBankAsync_WithoutAName_ThrowsAndWritesNothing(string? name)
    {
        var request = new BankCreateDTO { Name = name!, RoundUpEnabled = false };

        var act = async () => await _sut.CreateBankAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateBankAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        _repository.Banks.Add(Bank.Create("Barclays", roundUpEnabled: false));
        var request = new BankCreateDTO { Name = "Barclays", RoundUpEnabled = true };

        var act = async () => await _sut.CreateBankAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateBankAsync_WithValidRequest_UpdatesAndSaves()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Banks.Add(bank);
        var request = new BankUpdateDTO { Name = "Chase", RoundUpEnabled = true };

        var result = await _sut.UpdateBankAsync(bank.Id, request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Chase");
            result.RoundUpEnabled.Should().BeTrue();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateBankAsync_ToItsOwnCurrentName_Succeeds()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Banks.Add(bank);
        var request = new BankUpdateDTO { Name = "Barclays", RoundUpEnabled = true };

        var result = await _sut.UpdateBankAsync(bank.Id, request);

        result.RoundUpEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBankAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var request = new BankUpdateDTO { Name = "Chase", RoundUpEnabled = true };

        var act = async () => await _sut.UpdateBankAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateBankAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        var barclays = Bank.Create("Barclays", roundUpEnabled: false);
        var chase = Bank.Create("Chase", roundUpEnabled: true);
        _repository.Banks.Add(barclays);
        _repository.Banks.Add(chase);
        var request = new BankUpdateDTO { Name = "Chase", RoundUpEnabled = false };

        var act = async () => await _sut.UpdateBankAsync(barclays.Id, request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteBankAsync_WithNoReferences_RemovesAndSaves()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Banks.Add(bank);

        await _sut.DeleteBankAsync(bank.Id);

        using (new AssertionScope())
        {
            _repository.Banks.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteBankAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteBankAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteBankAsync_ReferencedByBalanceAdjustment_ThrowsAndWritesNothing()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Banks.Add(bank);
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 7, 1), bank, 100m, 0m, "Test adjustment"));

        var act = async () => await _sut.DeleteBankAsync(bank.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>();
            _repository.Banks.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteBankAsync_ReferencedByIncome_ThrowsAndWritesNothing()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Banks.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Gleison, null, 500m, bank));

        var act = async () => await _sut.DeleteBankAsync(bank.Id);

        await act.Should().ThrowAsync<EntityInUseException>();
    }

    [Fact]
    public async Task DeleteBankAsync_ReferencedByExpensePaymentSource_ThrowsAndWritesNothing()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Banks.Add(bank);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), bank, null));

        var act = async () => await _sut.DeleteBankAsync(bank.Id);

        await act.Should().ThrowAsync<EntityInUseException>();
    }

    [Fact]
    public async Task DeleteBankAsync_ReferencedByTransfer_ThrowsAndWritesNothing()
    {
        var source = Bank.Create("Barclays", roundUpEnabled: false);
        var destination = Bank.Create("Chase", roundUpEnabled: true);
        _repository.Banks.Add(source);
        _repository.Banks.Add(destination);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 1), source, destination, 50m, note: null));

        var act = async () => await _sut.DeleteBankAsync(source.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>();
            await Assert.ThrowsAsync<EntityInUseException>(() => _sut.DeleteBankAsync(destination.Id));
        }
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
