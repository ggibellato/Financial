using Financial.CashFlow.Application.DTOs;
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

public class BalanceAdjustmentServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<BalanceAdjustmentService> Logger = NullLogger<BalanceAdjustmentService>.Instance;
    private static readonly Microsoft.Extensions.Logging.ILogger<BankService> BankLogger = NullLogger<BankService>.Instance;

    private static IncomeSource Lottery => IncomeSource.Create("Lottery", IncomeGroup.NonReportable);

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly BalanceAdjustmentService _sut;

    public BalanceAdjustmentServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultBanks: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository or dependency does not repeat the whole construction sequence.</summary>
    private BalanceAdjustmentService CreateService(StubCashFlowRepository? repository = null, BankService? bankService = null) =>
        new(repository ?? _repository, bankService ?? new BankService(repository ?? _repository, _tracer, BankLogger), _tracer, Logger);

    private static Bank BankOf(StubCashFlowRepository repository, string name) =>
        repository.Banks.First(b => b.Name == name);

    private static Guid BankIdOf(StubCashFlowRepository repository, string name) =>
        BankOf(repository, name).Id;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new BalanceAdjustmentService(null!, new BankService(_repository, _tracer, BankLogger), _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullBankService_Throws()
    {
        Action act = () => new BalanceAdjustmentService(_repository, null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankService");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new BalanceAdjustmentService(
            _repository, new BankService(_repository, _tracer, BankLogger), null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithValidRequest_RecordsSuccessfulSpan()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var bankId = BankIdOf(_repository, "Barclays");

        var result = await _sut.AddAdjustmentAsync(bankId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m,
            Note = "Matched statement"
        });

        var span = _tracer.Spans.Should().Contain(s => s.Name == "CashFlow.BalanceAdjustmentService.AddAdjustment").Which;
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("CashFlow");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("BalanceAdjustment");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
        result.Should().NotBeNull();
    }

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

        // Balance as of date = 100 (opening) + 200 (income) - 50 (expense) = 250; delta = 240 - 250 = -10
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

        // Balance as of the second date = 100 (opening) + 50 (first adjustment's delta) = 150; target 130 => delta -20
        var second = await _sut.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 130m
        });

        second.Delta.Should().Be(-20m);
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithUnresolvableBank_ThrowsArgumentException()
    {
        var unknownBankId = Guid.NewGuid();

        var act = async () => await _sut.AddAdjustmentAsync(unknownBankId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{unknownBankId}' was not found*");
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithNegativeTargetBalance_ThrowsArgumentException()
    {
        var act = async () => await _sut.AddAdjustmentAsync(BankIdOf(_repository, "Barclays"), new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = -1m
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithUnresolvableBank_ThrowsArgumentException()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var added = await _sut.AddAdjustmentAsync(BankIdOf(_repository, "Barclays"), new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });
        var unknownBankId = Guid.NewGuid();

        var act = async () => await _sut.UpdateAdjustmentAsync(unknownBankId, added.Id, new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 120m
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{unknownBankId}' was not found*");
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
        }
    }

    [Fact]
    public async Task AddAdjustmentAsync_BankBalanceAsOfSameDateThenEqualsTargetBalanceExactly()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var barclays = BankOf(_repository, "Barclays");
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 37m, barclays));
        var bankService = new BankService(_repository, _tracer, BankLogger);
        var service = CreateService(bankService: bankService);

        await service.AddAdjustmentAsync(barclays.Id, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 240m
        });

        bankService.GetBankBalanceAsOf(barclays.Id, new DateOnly(2026, 7, 25)).Should().Be(240m);
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithDateMovedEarlierThanItsPreviousDate_ComputesCorrectDelta()
    {
        // Regresses the scenario an "add back the old delta" approach would get wrong: since the
        // adjustment's new date (07-01) is earlier than its own previous date (07-20), a balance
        // computed as of the new date using the pre-update entity would not have included the old
        // delta at all (07-20 > 07-01), so blindly adding it back would double it into the result.
        // Excluding the adjustment by id, regardless of date, sidesteps that entirely.
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
    public async Task UpdateAdjustmentAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateAdjustmentAsync(BankIdOf(_repository, "Barclays"), Guid.NewGuid(), new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
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
    }

    [Fact]
    public async Task DeleteAdjustmentAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteAdjustmentAsync(BankIdOf(_repository, "Barclays"), Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetAdjustmentsByBank_ReturnsOnlyThatBanksAdjustments()
    {
        _repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        _repository.SetOpeningBalance("Chase", 100m, new DateOnly(2026, 1, 1));
        await _sut.AddAdjustmentAsync(BankIdOf(_repository, "Barclays"), new BalanceAdjustmentCreateDTO { Date = new DateOnly(2026, 7, 1), TargetBalance = 150m });
        await _sut.AddAdjustmentAsync(BankIdOf(_repository, "Chase"), new BalanceAdjustmentCreateDTO { Date = new DateOnly(2026, 7, 1), TargetBalance = 120m });

        var result = _sut.GetAdjustmentsByBank(BankIdOf(_repository, "Barclays"));

        result.Should().ContainSingle().Which.BankName.Should().Be("Barclays");
    }

    [Fact]
    public void GetAdjustmentsByBank_WithUnrecognizedBank_ReturnsEmptyList()
    {
        var result = _sut.GetAdjustmentsByBank(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new BalanceAdjustmentService(_repository, new BankService(_repository, _tracer, BankLogger), _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
