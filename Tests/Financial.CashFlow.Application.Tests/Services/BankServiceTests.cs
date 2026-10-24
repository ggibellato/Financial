using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
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
    public void GetBanks_MapsEveryRepositoryBankToADto()
    {
        _repository.Accounts.Add(TestAccounts.Bank("Barclays", roundUpEnabled: false));
        _repository.Accounts.Add(TestAccounts.Bank("Trading212", roundUpEnabled: true));

        var result = _sut.GetBanks();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            result.Should().ContainSingle(b => b.Name == "Barclays" && !b.RoundUpEnabled);
            result.Should().ContainSingle(b => b.Name == "Trading212" && b.RoundUpEnabled);
        }
    }

    [Fact]
    public void GetBanks_InactiveBank_IsListedAsInactive()
    {
        var closed = TestAccounts.Bank("Closed");
        closed.SetActive(false);
        _repository.Accounts.Add(TestAccounts.Bank("Barclays"));
        _repository.Accounts.Add(closed);

        var result = _sut.GetBanks();

        result.Select(bank => (bank.Name, bank.IsActive)).Should().Equal(("Barclays", true), ("Closed", false));
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
            _repository.Accounts.Should().ContainSingle(b => b.Name == "Barclays");
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

    [Theory]
    [InlineData("Barclays")]
    [InlineData("barclays")]
    [InlineData(" BARCLAYS ")]
    public async Task CreateBankAsync_WithDuplicateNameInAnyCase_ThrowsAndWritesNothing(string name)
    {
        _repository.Accounts.Add(TestAccounts.Bank("Barclays", roundUpEnabled: false));
        var request = new BankCreateDTO { Name = name, RoundUpEnabled = true };

        var act = async () => await _sut.CreateBankAsync(request);

        using (new AssertionScope())
        {
            (await act.Should().ThrowAsync<DuplicateNameException>()).Which.Message.Should().StartWith("A bank named");
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateBankAsync_WithValidRequest_UpdatesAndSaves()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
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
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
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
        var barclays = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        var chase = TestAccounts.Bank("Chase", roundUpEnabled: true);
        _repository.Accounts.Add(barclays);
        _repository.Accounts.Add(chase);
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
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        _repository.Accounts.Add(bank);

        await _sut.DeleteBankAsync(bank.Id);

        using (new AssertionScope())
        {
            _repository.Accounts.Should().BeEmpty();
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
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 7, 1), bank, 100m, 0m, "Test adjustment"));

        var act = async () => await _sut.DeleteBankAsync(bank.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>();
            _repository.Accounts.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteBankAsync_ReferencedByIncome_ThrowsAndWritesNothing()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Gleison, null, 500m, bank));

        var act = async () => await _sut.DeleteBankAsync(bank.Id);

        await act.Should().ThrowAsync<EntityInUseException>();
    }

    [Fact]
    public async Task DeleteBankAsync_ReferencedByExpensePaymentSource_ThrowsAndWritesNothing()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), bank, null));

        var act = async () => await _sut.DeleteBankAsync(bank.Id);

        await act.Should().ThrowAsync<EntityInUseException>();
    }

    [Fact]
    public async Task DeleteBankAsync_ReferencedByTransfer_ThrowsAndWritesNothing()
    {
        var source = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        var destination = TestAccounts.Bank("Chase", roundUpEnabled: true);
        _repository.Accounts.Add(source);
        _repository.Accounts.Add(destination);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 1), source, destination, 50m, destinationAmount: null, note: null, today: TestClock.Today));

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
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
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
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
        var request = new BankOpeningBalanceUpdateDTO { OpeningBalance = -1m, OpeningBalanceDate = new DateOnly(2026, 7, 1) };

        var act = async () => await _sut.UpdateOpeningBalanceAsync(bank.Id, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetBankBalancesByMonth_CombinesOpeningBalanceIncomeAndExpenses()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Gleison, null, 500m, bank));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), bank, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 550m);
    }

    [Fact]
    public void GetBankBalancesByMonth_AddsRoundUpAmountToExpenseValue()
    {
        var bank = TestAccounts.Bank("Trading212", roundUpEnabled: true);
        bank.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(bank);
        var expense = Expense.Create(new DateOnly(2026, 7, 5), "TfL", 9.40m, Category.Create("Extras"), bank, null);
        expense.SetRoundUpAmount(0.60m);
        _repository.Expenses.Add(expense);

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Trading212" && b.Balance == -10.00m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesActivityBeforeOpeningBalanceDate()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        _repository.Accounts.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 6, 30), Gleison, null, 500m, bank));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 6, 30), "Groceries", 50m, Category.Create("Mercado"), bank, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesActivityAfterSelectedMonth()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 8, 1), Gleison, null, 500m, bank));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_WithNoActivity_ReturnsOpeningBalance()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(250m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(bank);

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 250m);
    }

    [Fact]
    public void GetBankBalancesByMonth_IgnoresActivityTaggedToADifferentBank()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(bank);
        var chase = TestAccounts.Bank("Chase", roundUpEnabled: false);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Gleison, null, 500m, chase));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), chase, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 0m);
    }

    [Fact]
    public void GetBankBalancesByMonth_AddsTransferAmountToDestinationBank()
    {
        var barclays = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        barclays.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        var trading212 = TestAccounts.Bank("Trading212", roundUpEnabled: false);
        trading212.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(barclays);
        _repository.Accounts.Add(trading212);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 5), barclays, trading212, 500m, destinationAmount: null, null, today: TestClock.Today));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Trading212" && b.Balance == 500m);
    }

    [Fact]
    public void GetBankBalancesByMonth_SubtractsTransferAmountFromSourceBank()
    {
        var barclays = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        barclays.SetOpeningBalance(1000m, new DateOnly(2026, 1, 1));
        var trading212 = TestAccounts.Bank("Trading212", roundUpEnabled: false);
        trading212.SetOpeningBalance(0m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(barclays);
        _repository.Accounts.Add(trading212);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 5), barclays, trading212, 500m, destinationAmount: null, null, today: TestClock.Today));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 500m);
    }

    [Fact]
    public void GetBankBalancesByMonth_IgnoresTransferTouchingNeitherRoleForTheBank()
    {
        var barclays = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        barclays.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        var trading212 = TestAccounts.Bank("Trading212", roundUpEnabled: false);
        var chase = TestAccounts.Bank("Chase", roundUpEnabled: false);
        _repository.Accounts.Add(barclays);
        _repository.Accounts.Add(trading212);
        _repository.Accounts.Add(chase);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 5), trading212, chase, 500m, destinationAmount: null, null, today: TestClock.Today));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_AddsBalanceAdjustmentDelta()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(bank);
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 7, 5), bank, 150m, 50m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 150m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesTransferAndAdjustmentAfterTheAsOfDate()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        var trading212 = TestAccounts.Bank("Trading212", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
        _repository.Accounts.Add(trading212);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 8, 1), bank, trading212, 500m, destinationAmount: null, null, today: TestClock.Today));
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 8, 1), bank, 999m, 899m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalancesByMonth_ExcludesTransferAndAdjustmentBeforeOpeningBalanceDate()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        var trading212 = TestAccounts.Bank("Trading212", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
        _repository.Accounts.Add(trading212);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 6, 30), bank, trading212, 500m, destinationAmount: null, null, today: TestClock.Today));
        _repository.BalanceAdjustments.Add(BalanceAdjustment.Create(new DateOnly(2026, 6, 30), bank, 999m, 899m, null));

        var result = _sut.GetBankBalancesByMonth(2026, 7);

        result.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 100m);
    }

    [Fact]
    public void GetBankBalanceAsOf_ComputesBalanceForAnArbitraryDateIndependentOfMonthBoundaries()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(bank);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 10), Gleison, null, 200m, bank));
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 20), Gleison, null, 300m, bank));

        var result = _sut.GetBankBalanceAsOf(bank.Id, new DateOnly(2026, 7, 15));

        result.Should().Be(300m);
    }

    [Fact]
    public void GetBankBalanceAsOf_WithExcludingAdjustmentId_OmitsOnlyThatAdjustment()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(bank);
        var chase = TestAccounts.Bank("Chase", roundUpEnabled: false);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 1), bank, chase, 20m, destinationAmount: null, null, today: TestClock.Today));
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
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(bank);
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
    public void GetBanks_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetBanks();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.BankService.GetBanks");
    }

    [Fact]
    public async Task UpdateBankAsync_WithBlankName_ThrowsArgumentException()
    {
        var bank = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        _repository.Accounts.Add(bank);
        var request = new BankUpdateDTO { Name = "   ", RoundUpEnabled = true };

        var act = async () => await _sut.UpdateBankAsync(bank.Id, request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*name is required*");
    }

    [Fact]
    public void GetBankBalancesByMonth_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetBankBalancesByMonth(2026, 7);

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.BankService.GetBankBalancesByMonth");
    }

    [Fact]
    public void GetBanks_WithATransferReferencingABank_MarksItAsReferenced()
    {
        var source = TestAccounts.Bank("Barclays", roundUpEnabled: false);
        var destination = TestAccounts.Bank("Chase", roundUpEnabled: true);
        _repository.Accounts.Add(source);
        _repository.Accounts.Add(destination);
        _repository.Transfers.Add(Transfer.Create(new DateOnly(2026, 7, 1), source, destination, 50m, destinationAmount: null, note: null, today: TestClock.Today));

        var result = _sut.GetBanks();

        using (new AssertionScope())
        {
            result.Should().ContainSingle(b => b.Id == source.Id).Which.HasReferences.Should().BeTrue();
            result.Should().ContainSingle(b => b.Id == destination.Id).Which.HasReferences.Should().BeTrue();
        }
    }
}
