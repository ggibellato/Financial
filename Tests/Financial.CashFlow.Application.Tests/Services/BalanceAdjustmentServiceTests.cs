using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class BalanceAdjustmentServiceTests
{
    private static IncomeSource Lottery => IncomeSource.Create("Lottery", IncomeGroup.NonReportable);

    private static Bank BankOf(StubCashFlowRepository repository, string name) =>
        repository.Banks.First(b => b.Name == name);

    private static Guid BankIdOf(StubCashFlowRepository repository, string name) =>
        BankOf(repository, name).Id;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new BalanceAdjustmentService(null!, new BankService(new StubCashFlowRepository()));
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullBankService_Throws()
    {
        Action act = () => new BalanceAdjustmentService(new StubCashFlowRepository(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankService");
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithNoPriorActivity_ComputesDeltaAgainstOpeningBalance()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository, new BankService(repository));

        var result = await service.AddAdjustmentAsync(BankIdOf(repository, "Barclays"), new BalanceAdjustmentCreateDTO
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
            repository.BalanceAdjustments.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithPriorIncomesAndExpenses_ComputesCorrectDelta()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var barclays = BankOf(repository, "Barclays");
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 200m, barclays));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Mercado, barclays, null));
        var service = new BalanceAdjustmentService(repository, new BankService(repository));

        var result = await service.AddAdjustmentAsync(barclays.Id, new BalanceAdjustmentCreateDTO
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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository, new BankService(repository));
        var barclaysId = BankIdOf(repository, "Barclays");
        var first = await service.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 6, 1),
            TargetBalance = 150m
        });
        first.Delta.Should().Be(50m);

        // Balance as of the second date = 100 (opening) + 50 (first adjustment's delta) = 150; target 130 => delta -20
        var second = await service.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 130m
        });

        second.Delta.Should().Be(-20m);
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithUnresolvableBank_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new BalanceAdjustmentService(repository, new BankService(repository));
        var unknownBankId = Guid.NewGuid();

        var act = async () => await service.AddAdjustmentAsync(unknownBankId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{unknownBankId}' was not found*");
    }

    [Fact]
    public async Task AddAdjustmentAsync_WithNegativeTargetBalance_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new BalanceAdjustmentService(repository, new BankService(repository));

        var act = async () => await service.AddAdjustmentAsync(BankIdOf(repository, "Barclays"), new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = -1m
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithUnresolvableBank_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository, new BankService(repository));
        var added = await service.AddAdjustmentAsync(BankIdOf(repository, "Barclays"), new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });
        var unknownBankId = Guid.NewGuid();

        var act = async () => await service.UpdateAdjustmentAsync(unknownBankId, added.Id, new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 120m
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*Bank '{unknownBankId}' was not found*");
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithExistingId_RecomputesAndPersistsDelta()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository, new BankService(repository));
        var barclaysId = BankIdOf(repository, "Barclays");
        var added = await service.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });
        added.Delta.Should().Be(50m);

        var result = await service.UpdateAdjustmentAsync(barclaysId, added.Id, new BalanceAdjustmentUpdateDTO
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
            repository.BalanceAdjustments.Should().ContainSingle();
            repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task AddAdjustmentAsync_BankBalanceAsOfSameDateThenEqualsTargetBalanceExactly()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var barclays = BankOf(repository, "Barclays");
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 37m, barclays));
        var bankService = new BankService(repository);
        var service = new BalanceAdjustmentService(repository, bankService);

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
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository, new BankService(repository));
        var barclaysId = BankIdOf(repository, "Barclays");
        var added = await service.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 20),
            TargetBalance = 200m
        });
        added.Delta.Should().Be(100m);

        var result = await service.UpdateAdjustmentAsync(barclaysId, added.Id, new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            TargetBalance = 150m
        });

        result.Delta.Should().Be(50m);
    }

    [Fact]
    public async Task UpdateAdjustmentAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new BalanceAdjustmentService(repository, new BankService(repository));

        var act = async () => await service.UpdateAdjustmentAsync(BankIdOf(repository, "Barclays"), Guid.NewGuid(), new BalanceAdjustmentUpdateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 100m
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAdjustmentAsync_WithExistingId_RemovesAndSaves()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository, new BankService(repository));
        var barclaysId = BankIdOf(repository, "Barclays");
        var added = await service.AddAdjustmentAsync(barclaysId, new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            TargetBalance = 150m
        });

        await service.DeleteAdjustmentAsync(barclaysId, added.Id);

        repository.BalanceAdjustments.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAdjustmentAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new BalanceAdjustmentService(repository, new BankService(repository));

        var act = async () => await service.DeleteAdjustmentAsync(BankIdOf(repository, "Barclays"), Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetAdjustmentsByBank_ReturnsOnlyThatBanksAdjustments()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.SetOpeningBalance("Barclays", 100m, new DateOnly(2026, 1, 1));
        repository.SetOpeningBalance("Chase", 100m, new DateOnly(2026, 1, 1));
        var service = new BalanceAdjustmentService(repository, new BankService(repository));
        await service.AddAdjustmentAsync(BankIdOf(repository, "Barclays"), new BalanceAdjustmentCreateDTO { Date = new DateOnly(2026, 7, 1), TargetBalance = 150m });
        await service.AddAdjustmentAsync(BankIdOf(repository, "Chase"), new BalanceAdjustmentCreateDTO { Date = new DateOnly(2026, 7, 1), TargetBalance = 120m });

        var result = service.GetAdjustmentsByBank(BankIdOf(repository, "Barclays"));

        result.Should().ContainSingle().Which.BankName.Should().Be("Barclays");
    }

    [Fact]
    public void GetAdjustmentsByBank_WithUnrecognizedBank_ReturnsEmptyList()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        var service = new BalanceAdjustmentService(repository, new BankService(repository));

        var result = service.GetAdjustmentsByBank(Guid.NewGuid());

        result.Should().BeEmpty();
    }
}
