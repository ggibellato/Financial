using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class BalanceAdjustmentServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<BalanceAdjustmentService> Logger = NullLogger<BalanceAdjustmentService>.Instance;

    private static IncomeSource Lottery => IncomeSource.Create("Lottery", IncomeGroup.NonReportable);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly BalanceAdjustmentService _sut;

    public BalanceAdjustmentServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultBanks: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = new BalanceAdjustmentService(_repository, _tracer, Logger);
    }

    private static Account BankOf(StubCashFlowRepository repository, string name) =>
        repository.Accounts.First(b => b.Name == name);

    private static Guid BankIdOf(StubCashFlowRepository repository, string name) =>
        BankOf(repository, name).Id;

    [Fact]
    public async Task AddAdjustmentAsync_WithNoPriorActivity_ComputesDeltaAgainstOpeningBalance()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));

        var result = await _sut.AddAdjustmentAsync(BankIdOf(_repository, "Barclays"), new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m,
            Note = "Matched statement"
        });

        using (new AssertionScope())
        {
            result.BankName.Should().Be("Barclays");
            result.TargetBalance.Should().Be(150m);
            result.Delta.Should().Be(50m);
            result.Note.Should().Be("Matched statement");
            _repository.BalanceAdjustments.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(1);
            var span = _tracer.ShouldHaveSucceededSpan("CashFlow.BalanceAdjustmentService.AddAdjustment");
            span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("BalanceAdjustment");
            span.Attributes[TelemetryAttributeKeys.EntityId].Should().Be(BankIdOf(_repository, "Barclays").ToString());
        }
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithPriorIncomesAndExpenses_ComputesCorrectDelta()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var barclays = BankOf(_repository, "Barclays");
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 200m, barclays));
        _repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Create("Mercado"), barclays, null));

        var result = await _sut.AddAdjustmentAsync(barclays.Id, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 240m
        });

        result.Delta.Should().Be(-10m);
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithExistingAdjustmentForSameBank_StacksDelta()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var barclaysId = BankIdOf(_repository, "Barclays");
        var first = await _sut.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 6, 1),
            TargetBalance = 150m
        });
        first.Delta.Should().Be(50m);

        var second = await _sut.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 130m
        });

        second.Delta.Should().Be(-20m);
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithUnknownAccount_ThrowsKeyNotFoundException()
    {
        var unknownAccountId = Guid.NewGuid();

        var act = async () => await _sut.AddAdjustmentAsync(unknownAccountId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*Account '{unknownAccountId}' was not found*");
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.BalanceAdjustmentService.AddAdjustment");
    }

    [Fact]
    public async Task AddAdjustmentAsync_OnAnAccountWithoutTheBankTemplate_UsesThatAccountsBalance()
    {
        var account = Account.CreateWithCapabilities("Custom", Currency.GBP, new HashSet<Capability> { Capability.CanReconcile, Capability.HasOpeningBalance }, template: AccountTemplate.Pots);
        account.SetOpeningBalance(70m, new DateOnly(2026, 1, 1));
        _repository.Accounts.Add(account);

        var result = await _sut.AddAdjustmentAsync(account.Id, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        });

        result.Delta.Should().Be(30m);
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithAnAdjustmentOfAnotherAccount_ThrowsKeyNotFoundException()
    {
        var added = await _sut.AddAdjustmentAsync(BankIdOf(_repository, "Barclays"), new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });

        var act = async () => await _sut.UpdateAdjustmentAsync(BankIdOf(_repository, "Chase"), added.Id, new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 120m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*Balance adjustment '{added.Id}' was not found*");
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithUnknownAccount_ThrowsKeyNotFoundException()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var added = await _sut.AddAdjustmentAsync(BankIdOf(_repository, "Barclays"), new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });
        var unknownAccountId = Guid.NewGuid();

        var act = async () => await _sut.UpdateAdjustmentAsync(unknownAccountId, added.Id, new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 120m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*Account '{unknownAccountId}' was not found*");
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.BalanceAdjustmentService.UpdateAdjustment");
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithExistingId_RecomputesAndPersistsDelta()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var barclaysId = BankIdOf(_repository, "Barclays");
        var added = await _sut.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });
        added.Delta.Should().Be(50m);

        var result = await _sut.UpdateAdjustmentAsync(barclaysId, added.Id, new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 120m,
            Note = "Corrected"
        });

        using (new AssertionScope())
        {
            result.Id.Should().Be(added.Id);
            result.TargetBalance.Should().Be(120m);
            result.Delta.Should().Be(20m);
            result.Note.Should().Be("Corrected");
            _repository.BalanceAdjustments.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(2);
            _tracer.ShouldHaveSucceededSpan("CashFlow.BalanceAdjustmentService.UpdateAdjustment").Attributes[TelemetryAttributeKeys.EntityId].Should().Be(added.Id.ToString());
        }
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithDateMovedEarlierThanItsPreviousDate_ComputesCorrectDelta()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var barclaysId = BankIdOf(_repository, "Barclays");
        var added = await _sut.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 20),
            TargetBalance = 200m
        });
        added.Delta.Should().Be(100m);

        var result = await _sut.UpdateAdjustmentAsync(barclaysId, added.Id, new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 150m
        });

        result.Delta.Should().Be(50m);
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithAnotherAdjustmentOnTheAccount_KeepsItsDelta()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var barclaysId = BankIdOf(_repository, "Barclays");
        await _sut.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO { Date = new DateOnly(2026, 6, 1), TargetBalance = 150m });
        var edited = await _sut.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO { Date = new DateOnly(2026, 7, 1), TargetBalance = 130m });

        var result = await _sut.UpdateAdjustmentAsync(barclaysId, edited.Id, new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 200m
        });

        result.Delta.Should().Be(50m);
    }

    [Fact]
    public async Task DeleteAdjustmentAsync_WithExistingId_RemovesAndSaves()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var barclaysId = BankIdOf(_repository, "Barclays");
        var added = await _sut.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });

        await _sut.DeleteAdjustmentAsync(barclaysId, added.Id);

        _repository.BalanceAdjustments.Should().BeEmpty();
        _repository.SaveChangesCallCount.Should().Be(2);
        _tracer.ShouldHaveSucceededSpan("CashFlow.BalanceAdjustmentService.DeleteAdjustment").Attributes[TelemetryAttributeKeys.EntityId].Should().Be(added.Id.ToString());
    }

    [Fact]
    public async Task DeleteAdjustmentAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteAdjustmentAsync(BankIdOf(_repository, "Barclays"), Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("CashFlow.BalanceAdjustmentService.DeleteAdjustment");
    }
}
