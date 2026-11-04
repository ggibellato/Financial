using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class IncomeServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<IncomeService> Logger = NullLogger<IncomeService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly IncomeService _sut;

    public IncomeServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private IncomeService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public async Task AddIncomeAsync_WithValidRequest_RecordsSuccessfulSpan()
    {
        var result = await _sut.AddIncomeAsync(ToCreateDto(_repository, ValidCreateRequest()));

        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("CashFlow.IncomeService.AddIncome");
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("CashFlow");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("Income");
        span.Attributes[TelemetryAttributeKeys.EntityId].Should().Be(result.Id.ToString());
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task AddIncomeAsync_WithValidRequest_SavesAndReturnsIncome()
    {
        var result = await _sut.AddIncomeAsync(ToCreateDto(_repository, ValidCreateRequest()));

        using (new AssertionScope())
        {
            result.Date.Should().Be(new DateOnly(2026, 7, 25));
            result.IncomeSourceName.Should().Be("Gleison");
            result.GrossValue.Should().Be(3200.00m);
            result.NetValue.Should().Be(2450.00m);
            result.BankName.Should().Be("Barclays");
            _repository.Incomes.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithoutGrossValue_SavesNull()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { GrossValue = null });

        var result = await _sut.AddIncomeAsync(request);

        result.GrossValue.Should().BeNull();
    }

    [Fact]
    public async Task AddIncomeAsync_MultipleEntriesForSameSourceAndMonth_AllPersist()
    {
        var request = ValidCreateRequest() with { IncomeSource = "Ariana", GrossValue = null };

        await _sut.AddIncomeAsync(ToCreateDto(_repository, request with { Date = new DateOnly(2026, 7, 1) }));
        await _sut.AddIncomeAsync(ToCreateDto(_repository, request with { Date = new DateOnly(2026, 7, 8) }));
        await _sut.AddIncomeAsync(ToCreateDto(_repository, request with { Date = new DateOnly(2026, 7, 15) }));

        _repository.Incomes.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddIncomeAsync_WithNegativeNetValue_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { GrossValue = null, NetValue = -1m });

        var act = async () => await _sut.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddIncomeAsync_WithUnrecognizedIncomeSource_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { IncomeSource = "NotASource" });

        var act = async () => await _sut.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Income source*not recognized*");
    }

    [Fact]
    public async Task AddIncomeAsync_WithInactiveIncomeSource_Succeeds()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true);
        repository.IncomeSources.Add(IncomeSource.Create("RetiredSource", IncomeGroup.NonReportable, isActive: false));
        var service = CreateService(repository);
        var request = ToCreateDto(repository, ValidCreateRequest() with { IncomeSource = "RetiredSource" });

        var result = await service.AddIncomeAsync(request);

        result.IncomeSourceName.Should().Be("RetiredSource");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddIncomeAsync_WithBlankIncomeSource_ThrowsArgumentException(string? incomeSource)
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { IncomeSource = incomeSource! });

        var act = async () => await _sut.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddIncomeAsync_WithUnrecognizedBank_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Bank = "NotABank" });

        var act = async () => await _sut.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Bank*not recognized*");
    }

    [Fact]
    public async Task AddIncomeAsync_WithoutBank_Succeeds()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Bank = null });

        var result = await _sut.AddIncomeAsync(request);

        using (new AssertionScope())
        {
            result.BankId.Should().BeNull();
            result.BankName.Should().BeNull();
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithDescription_SavesDescription()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Description = "Chip ISA dividend" });

        var result = await _sut.AddIncomeAsync(request);

        result.Description.Should().Be("Chip ISA dividend");
    }

    [Fact]
    public async Task AddIncomeAsync_WithoutDescription_DescriptionIsNull()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest());

        var result = await _sut.AddIncomeAsync(request);

        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task AddIncomeAsync_WithDescriptionOver200Characters_ThrowsArgumentException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Description = new string('a', 201) });

        var act = async () => await _sut.AddIncomeAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*200 characters*");
    }

    [Fact]
    public async Task AddIncomeAsync_WithDescriptionExactly200Characters_Succeeds()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Description = new string('a', 200) });

        var result = await _sut.AddIncomeAsync(request);

        result.Description.Should().HaveLength(200);
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithExistingId_UpdatesInPlace()
    {
        var added = await _sut.AddIncomeAsync(ToCreateDto(_repository, ValidCreateRequest()));

        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { NetValue = 500m, GrossValue = null, IncomeSource = "Lottery" });
        var result = await _sut.UpdateIncomeAsync(added.Id, updateRequest);

        using (new AssertionScope())
        {
            result.Id.Should().Be(added.Id);
            result.NetValue.Should().Be(500m);
            result.IncomeSourceName.Should().Be("Lottery");
            _repository.Incomes.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithUnrecognizedIncomeSource_ThrowsArgumentException()
    {
        var added = await _sut.AddIncomeAsync(ToCreateDto(_repository, ValidCreateRequest()));

        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { IncomeSource = "NotASource" });
        var act = async () => await _sut.UpdateIncomeAsync(added.Id, updateRequest);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Income source*not recognized*");
    }

    [Fact]
    public async Task UpdateIncomeAsync_RemovingBank_SetsBankNull()
    {
        var added = await _sut.AddIncomeAsync(ToCreateDto(_repository, ValidCreateRequest()));

        var updateRequest = ToUpdateDto(_repository, ValidCreateRequest() with { Bank = null });
        var result = await _sut.UpdateIncomeAsync(added.Id, updateRequest);

        using (new AssertionScope())
        {
            result.BankId.Should().BeNull();
            result.BankName.Should().BeNull();
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateIncomeAsync(Guid.NewGuid(), ToUpdateDto(_repository, ValidCreateRequest()));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteIncomeAsync_WithExistingId_RemovesAndSaves()
    {
        var added = await _sut.AddIncomeAsync(ToCreateDto(_repository, ValidCreateRequest()));

        await _sut.DeleteIncomeAsync(added.Id);

        _repository.Incomes.Should().BeEmpty();
        _repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteIncomeAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteIncomeAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetIncomesByMonth_ReturnsOnlyIncomesInThatMonth()
    {
        await _sut.AddIncomeAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 7, 10) }));
        await _sut.AddIncomeAsync(ToCreateDto(_repository, ValidCreateRequest() with { Date = new DateOnly(2026, 8, 10) }));

        var result = _sut.GetIncomesByMonth(2026, 7);

        result.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 7, 10));
    }

    private static IncomeCreateRequest ValidCreateRequest() => new(
        new DateOnly(2026, 7, 25),
        "Gleison",
        3200.00m,
        2450.00m,
        "Barclays",
        null);

    private static IncomeCreateDTO ToCreateDto(StubCashFlowRepository repository, IncomeCreateRequest r) => new()
    {
        Date = r.Date,
        IncomeSourceId = ResolveIncomeSourceId(repository, r.IncomeSource),
        GrossValue = r.GrossValue,
        NetValue = r.NetValue,
        BankId = ResolveBankId(repository, r.Bank),
        Description = r.Description,
        PotsAccountId = ResolveAccountId(repository, r.PotsAccount),
        FundingLines = r.FundingLines
    };

    private static IncomeUpdateDTO ToUpdateDto(StubCashFlowRepository repository, IncomeCreateRequest r) => new()
    {
        Date = r.Date,
        IncomeSourceId = ResolveIncomeSourceId(repository, r.IncomeSource),
        GrossValue = r.GrossValue,
        NetValue = r.NetValue,
        BankId = ResolveBankId(repository, r.Bank),
        Description = r.Description,
        PotsAccountId = ResolveAccountId(repository, r.PotsAccount),
        FundingLines = r.FundingLines
    };

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path.</summary>
    private static Guid ResolveIncomeSourceId(StubCashFlowRepository repository, string? incomeSourceName) =>
        repository.IncomeSources.FirstOrDefault(s => s.Name == incomeSourceName)?.Id ?? Guid.NewGuid();

    /// <summary>Null bank name means "no bank supplied"; an unresolvable non-null name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path.</summary>
    private static Guid? ResolveBankId(StubCashFlowRepository repository, string? bankName) =>
        bankName is null ? null : repository.Accounts.FirstOrDefault(b => b.Name == bankName)?.Id ?? Guid.NewGuid();

    private static Guid? ResolveAccountId(StubCashFlowRepository repository, string? accountName) =>
        accountName is null ? null : repository.Accounts.FirstOrDefault(a => a.Name == accountName)?.Id ?? Guid.NewGuid();

    private static Guid AccountId(StubCashFlowRepository repository, string accountName) =>
        repository.Accounts.Single(a => a.Name == accountName).Id;

    private sealed record IncomeCreateRequest(
        DateOnly Date, string IncomeSource, decimal? GrossValue, decimal NetValue, string? Bank, string? Description, string? PotsAccount = null,
        IReadOnlyList<FundingLineDTO>? FundingLines = null);

    private static StubCashFlowRepository CreateSplitCapableRepository()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true, seedDefaultPots: true);
        repository.Accounts.Single(a => a.Name == "Reserva").SetCategory(Category.Create("Reserva"));
        return repository;
    }

    private static IReadOnlyList<FundingLineDTO> FundingLines(StubCashFlowRepository repository, params (string Bank, decimal Amount)[] lines) =>
        lines.Select(line => new FundingLineDTO { BankId = AccountId(repository, line.Bank), Amount = line.Amount }).ToList();

    private static Expense HandTypedExpense(StubCashFlowRepository repository)
    {
        var expense = Expense.Create(
            new DateOnly(2026, 7, 25), "Reserva", 500m, Category.Create("Reserva"), repository.Accounts.Single(a => a.Name == "Barclays"), creditCard: null);
        repository.Expenses.Add(expense);
        return expense;
    }

    private static (StubCashFlowRepository Repository, Income Older) RepositoryWithOlderSplitIncome()
    {
        var repository = CreateSplitCapableRepository();
        var reserva = repository.Accounts.Single(a => a.Name == "Reserva");
        var older = Income.Create(
            new DateOnly(2026, 7, 25), repository.IncomeSources.Single(s => s.Name == "Ariana"), null, 2450m,
            repository.Accounts.Single(a => a.Name == "Barclays"), potsAccount: reserva);
        repository.Incomes.Add(older);
        repository.PotTransactions.Add(PotTransaction.Create(repository.ReservaPots[0], 2205m, older.Date, string.Empty, older));
        return (repository, older);
    }

    private static (StubCashFlowRepository Repository, Account Holidays) RepositoryWithArianaSplittingToHolidays()
    {
        var repository = CreateSplitCapableRepository();
        var holidays = TestAccounts.Pots("Holidays", category: Category.Create("Holidays"));
        holidays.AddPot(Pot.Create("Flights", 50m));
        holidays.AddPot(Pot.Create("Hotels", 50m));
        holidays.AddPot(Pot.Create("Retired", 0m, isActive: false));
        repository.Accounts.Add(holidays);
        var ariana = repository.IncomeSources.Single(s => s.Name == "Ariana");
        ariana.Update(ariana.Name, ariana.Group, ariana.IsActive, holidays);
        return (repository, holidays);
    }

    private static IncomeCreateRequest ValidArianaSplitRequest() => new(
        new DateOnly(2026, 7, 25), "Ariana", null, 2450.00m, "Barclays", null, PotsAccount: "Reserva");

    [Fact]
    public async Task AddIncomeAsync_WithSplitForEligibleSource_CreatesLinkedMovementsForEveryActivePot()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);

        var result = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));

        using (new AssertionScope())
        {
            result.PotsAccountId.Should().Be(AccountId(repository, "Reserva"));
            repository.PotTransactions.Should().HaveCount(4);
            var expectedAmounts = new Dictionary<string, decimal>
            {
                ["Investimento"] = 734.93m,
                ["HouseTreats"] = 734.93m,
                ["Ariana"] = 367.57m,
                ["Gleison"] = 367.57m
            };
            foreach (var (potName, expectedAmount) in expectedAmounts)
            {
                repository.PotTransactions.Should().ContainSingle(m => m.Pot.Name == potName)
                    .Which.Should().Match<PotTransaction>(m => m.Amount == expectedAmount && m.Income!.Id == result.Id);
            }
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithSplitForASourceSplittingToAnotherPotsAccount_SplitsIntoItsActivePotsOnly()
    {
        var (repository, holidays) = RepositoryWithArianaSplittingToHolidays();
        var service = CreateService(repository);

        var result = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { PotsAccount = "Holidays" }));

        using (new AssertionScope())
        {
            result.PotsAccountId.Should().Be(holidays.Id);
            repository.Incomes.Single().PotsAccount.Should().BeSameAs(holidays);
            repository.PotTransactions.Select(m => (m.Pot.Name, m.Amount)).Should().Equal(("Flights", 1102.50m), ("Hotels", 1102.50m));
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_StillSplit_RebuildsTheMovementsInTheAccountTheIncomeNames()
    {
        var (repository, holidays) = RepositoryWithArianaSplittingToHolidays();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { PotsAccount = "Holidays" }));

        await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { PotsAccount = "Holidays", NetValue = 1000m }));

        using (new AssertionScope())
        {
            repository.Incomes.Single().PotsAccount.Should().BeSameAs(holidays);
            repository.PotTransactions.Should().OnlyContain(m => holidays.OwnsPot(m.Pot.Id) && m.Income!.Id == added.Id);
            repository.PotTransactions.Select(m => m.Amount).Should().Equal(450m, 450m);
        }
    }

    [Fact]
    public async Task AddIncomeAsync_NamingAnotherPotsAccountThanTheSource_SplitsIntoTheOneItNames()
    {
        var (repository, _) = RepositoryWithArianaSplittingToHolidays();
        var service = CreateService(repository);

        var result = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));

        using (new AssertionScope())
        {
            result.PotsAccountId.Should().Be(AccountId(repository, "Reserva"));
            repository.PotTransactions.Should().HaveCount(4);
            repository.PotTransactions.Should().OnlyContain(m => repository.ReservaPots.Contains(m.Pot));
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_AfterTheSourceMovedToAnotherPotsAccount_KeepsTheAccountTheRequestNames()
    {
        var (repository, holidays) = RepositoryWithArianaSplittingToHolidays();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { PotsAccount = "Holidays" }));
        var ariana = repository.IncomeSources.Single(s => s.Name == "Ariana");
        ariana.Update(ariana.Name, ariana.Group, ariana.IsActive, repository.Accounts.Single(a => a.Name == "Reserva"));

        var result = await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { PotsAccount = "Holidays", NetValue = 1000m }));

        using (new AssertionScope())
        {
            result.PotsAccountId.Should().Be(holidays.Id);
            repository.PotTransactions.Should().OnlyContain(m => holidays.OwnsPot(m.Pot.Id));
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithAnUnknownPotsAccountId_ThrowsAndPersistsNothing()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);

        var act = async () => await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { PotsAccount = "Nowhere" }));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Pots account '*' is not recognized.");
            repository.Incomes.Should().BeEmpty();
            repository.PotTransactions.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.IncomeService.AddIncome");
        }
    }

    [Fact]
    public async Task AddIncomeAsync_NamingAnAccountThatCannotReceiveAnIncomeSplit_ThrowsAndPersistsNothing()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);

        var act = async () => await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { PotsAccount = "Barclays" }));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Barclays can't receive an income split.");
            repository.Incomes.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.IncomeService.AddIncome");
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithSplitForIneligibleSource_ThrowsAndPersistsNothing()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var request = ValidArianaSplitRequest() with { IncomeSource = "Gleison" };

        var act = async () => await service.AddIncomeAsync(ToCreateDto(repository, request));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Gleison doesn't split income to pots.");
            repository.Incomes.Should().BeEmpty();
            repository.PotTransactions.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.IncomeService.AddIncome");
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithSplitAfterTheSourceStoppedSplitting_ThrowsAndKeepsTheMovements()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));
        var ariana = repository.IncomeSources.Single(s => s.Name == "Ariana");
        ariana.Update(ariana.Name, ariana.Group, ariana.IsActive, potsAccount: null);

        var act = async () => await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { NetValue = 1000m }));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Ariana doesn't split income to pots.");
            repository.Incomes.Single().NetValue.Should().Be(2450.00m);
            repository.PotTransactions.Should().HaveCount(4);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.IncomeService.UpdateIncome");
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithSplitAndZeroActivePots_ThrowsAndPersistsNothing()
    {
        var repository = new StubCashFlowRepository(seedDefaultBanks: true, seedDefaultIncomeSources: true);
        var service = CreateService(repository);

        var act = async () => await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Reserva has no active pots.");
            repository.Incomes.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WhenSaveFails_RecordsFailedSpanAndRethrows()
    {
        var repository = CreateSplitCapableRepository();
        repository.ThrowOnNextSave = true;
        var service = CreateService(repository);

        var act = async () => await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.IncomeService.AddIncome");
    }

    [Fact]
    public async Task UpdateIncomeAsync_TogglingSplitOff_DeletesLinkedMovements()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));

        var result = await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { PotsAccount = null }));

        using (new AssertionScope())
        {
            result.PotsAccountId.Should().BeNull();
            repository.Incomes.Single().PotsAccount.Should().BeNull();
            repository.PotTransactions.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_TogglingSplitOn_CreatesLinkedMovements()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { PotsAccount = null }));
        repository.PotTransactions.Should().BeEmpty();

        var result = await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest()));

        using (new AssertionScope())
        {
            result.PotsAccountId.Should().Be(AccountId(repository, "Reserva"));
            repository.PotTransactions.Should().HaveCount(4);
            repository.PotTransactions.Should().OnlyContain(m => m.Income!.Id == added.Id);
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_StillSplitWithChangedNetValue_RecreatesMovementsWithNewAmounts()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));
        var originalMovementIds = repository.PotTransactions.Select(m => m.Id).ToList();

        var result = await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { NetValue = 1000.00m }));

        using (new AssertionScope())
        {
            result.NetValue.Should().Be(1000.00m);
            repository.PotTransactions.Should().HaveCount(4);
            repository.PotTransactions.Select(m => m.Id).Should().NotIntersectWith(originalMovementIds);
            var investimento = repository.ReservaPots.Single(b => b.Name == "Investimento");
            repository.PotTransactions.Should().ContainSingle(m => m.Pot == investimento)
                .Which.Amount.Should().Be(299.97m);
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_WhenSaveFails_RecordsFailedSpanAndRethrows()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));
        repository.ThrowOnNextSave = true;

        var act = async () => await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { NetValue = 1000.00m }));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.IncomeService.UpdateIncome");
    }

    [Fact]
    public async Task DeleteIncomeAsync_WithLinkedMovements_DeletesIncomeAndMovements()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));

        await service.DeleteIncomeAsync(added.Id);

        repository.Incomes.Should().BeEmpty();
        repository.PotTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteIncomeAsync_WithoutLinkedMovements_DeletesOnlyIncome()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var unrelatedMovement = PotTransaction.Create(repository.ReservaPots[0], 10m, new DateOnly(2026, 7, 1), "Manual entry");
        repository.PotTransactions.Add(unrelatedMovement);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { PotsAccount = null }));

        await service.DeleteIncomeAsync(added.Id);

        repository.Incomes.Should().BeEmpty();
        repository.PotTransactions.Should().ContainSingle().Which.Should().BeSameAs(unrelatedMovement);
    }

    [Fact]
    public async Task AddIncomeAsync_ForArianaSource_SplitsBecauseTheSourceNamesAPotsAccount()
    {
        var repository = CreateSplitCapableRepository();
        var arianaSource = repository.IncomeSources.Single(s => s.Name == "Ariana");
        arianaSource.PotsAccount.Should().NotBeNull("the migrator seeds Ariana with the Reserva Pots account");
        var service = CreateService(repository);

        var result = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));

        result.PotsAccountId.Should().Be(arianaSource.PotsAccount!.Id);
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-01")]
    public async Task AddIncomeAsync_WithSplit_WritesTheLinkedDepositInOneSave()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);

        var result = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));

        using (new AssertionScope())
        {
            repository.SaveChangesCallCount.Should().Be(1);
            var expense = repository.Expenses.Should().ContainSingle().Which;
            expense.Date.Should().Be(new DateOnly(2026, 7, 25));
            expense.Description.Should().Be("Deposit to Reserva \u2013 Ariana");
            expense.Value.Should().Be(2205.00m);
            expense.Category.Name.Should().Be("Reserva");
            expense.PaymentSourceAccount!.Name.Should().Be("Barclays");
            expense.CountsAsTithe.Should().BeFalse();
            expense.OperationId.Should().NotBeNull();
            repository.PotTransactions.Should().OnlyContain(m => m.OperationId == expense.OperationId && m.Income!.Id == result.Id);
            result.HasLinkedDeposit.Should().BeTrue();
            result.FundingLines.Select(line => (line.BankId, line.Amount)).Should().Equal((AccountId(repository, "Barclays"), 2205.00m));
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithoutASplit_WritesNoDeposit()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var lines = new[] { new FundingLineDTO { BankId = Guid.NewGuid(), Amount = 1m } };

        var result = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { PotsAccount = null, FundingLines = lines }));

        using (new AssertionScope())
        {
            repository.Expenses.Should().BeEmpty();
            repository.PotTransactions.Should().BeEmpty();
            result.HasLinkedDeposit.Should().BeFalse();
            result.FundingLines.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithSplitAndNoBank_ThrowsAndPersistsNothing()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);

        var act = async () => await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { Bank = null }));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Choose the bank this income landed in.");
            repository.Incomes.Should().BeEmpty();
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.IncomeService.AddIncome");
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-02")]
    public async Task AddIncomeAsync_WithHandSplitLines_WritesOneExpensePerBank()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var lines = FundingLines(repository, ("Barclays", 2200m), ("Chase", 5m));

        var result = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { FundingLines = lines }));

        using (new AssertionScope())
        {
            repository.Expenses.Select(e => (e.PaymentSourceAccount!.Name, e.Value)).Should().Equal(("Barclays", 2200m), ("Chase", 5m));
            result.FundingLines.Select(line => line.Amount).Should().Equal(2200m, 5m);
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithLinesThatDoNotAddUp_ThrowsAndPersistsNothing()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var lines = FundingLines(repository, ("Barclays", 2200m));

        var act = async () => await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { FundingLines = lines }));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Funding lines must add up to \u00a32205.00.");
            repository.Incomes.Should().BeEmpty();
            repository.Expenses.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task AddIncomeAsync_WithAnUnknownFundingBank_Throws()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var lines = new[] { new FundingLineDTO { BankId = Guid.NewGuid(), Amount = 2205m } };

        var act = async () => await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { FundingLines = lines }));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Payment source '*' is not recognized.");
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-04")]
    public async Task UpdateIncomeAsync_WithALinkedDeposit_RebuildsEveryRecordKeepingTheOperationId()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));
        var operationId = repository.Expenses.Single().OperationId;
        var oldIds = repository.Expenses.Select(e => e.Id).Concat(repository.PotTransactions.Select(m => m.Id)).ToList();

        var result = await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { NetValue = 1000m, Bank = "Chase" }));

        using (new AssertionScope())
        {
            var expense = repository.Expenses.Should().ContainSingle().Which;
            (expense.Value, expense.PaymentSourceAccount!.Name, expense.OperationId).Should().Be((900m, "Chase", operationId));
            repository.PotTransactions.Should().HaveCount(4).And.OnlyContain(m => m.OperationId == operationId);
            repository.PotTransactions.Sum(m => m.Amount).Should().Be(900m);
            repository.Expenses.Select(e => e.Id).Concat(repository.PotTransactions.Select(m => m.Id)).Should().NotIntersectWith(oldIds);
            result.FundingLines.Select(line => line.Amount).Should().Equal(900m);
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_WithHandSplitLinesThatNoLongerAddUp_ThrowsAndKeepsTheDeposit()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var lines = FundingLines(repository, ("Barclays", 2200m), ("Chase", 5m));
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { FundingLines = lines }));

        var act = async () => await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { NetValue = 1000m, FundingLines = lines }));

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Funding lines must add up to \u00a3900.00.");
            repository.Expenses.Select(e => e.Value).Should().Equal(2200m, 5m);
            repository.PotTransactions.Sum(m => m.Amount).Should().Be(2205m);
            _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.IncomeService.UpdateIncome");
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_OmittingTheLinesOfAHandSplitDeposit_GoesBackToTheDefaultLine()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var lines = FundingLines(repository, ("Barclays", 2200m), ("Chase", 5m));
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { FundingLines = lines }));

        await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest()));

        repository.Expenses.Select(e => (e.PaymentSourceAccount!.Name, e.Value)).Should().Equal(("Barclays", 2205m));
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-04")]
    public async Task UpdateIncomeAsync_TogglingSplitOff_RemovesEveryRecordOfTheDeposit()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));

        var result = await service.UpdateIncomeAsync(added.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { PotsAccount = null }));

        using (new AssertionScope())
        {
            repository.Expenses.Should().BeEmpty();
            repository.PotTransactions.Should().BeEmpty();
            result.HasLinkedDeposit.Should().BeFalse();
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-04")]
    public async Task DeleteIncomeAsync_WithALinkedDeposit_RemovesOnlyItsRecords()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        var added = await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));
        var otherExpense = HandTypedExpense(repository);

        await service.DeleteIncomeAsync(added.Id);

        using (new AssertionScope())
        {
            repository.Incomes.Should().BeEmpty();
            repository.PotTransactions.Should().BeEmpty();
            repository.Expenses.Should().ContainSingle().Which.Should().BeSameAs(otherExpense);
        }
    }

    [Fact]
    public async Task GetIncomesByMonth_ReturnsEachIncomesFundingLines()
    {
        var repository = CreateSplitCapableRepository();
        var service = CreateService(repository);
        await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest()));
        await service.AddIncomeAsync(ToCreateDto(repository, ValidArianaSplitRequest() with { PotsAccount = null }));

        var incomes = service.GetIncomesByMonth(2026, 7);

        incomes.Select(income => (income.HasLinkedDeposit, income.FundingLines.Count)).Should().Equal((true, 1), (false, 0));
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-10")]
    public async Task UpdateIncomeAsync_ForAnOlderSplit_RebuildsTheMovementsAndAddsNoExpense()
    {
        var (repository, older) = RepositoryWithOlderSplitIncome();
        var service = CreateService(repository);
        var handTyped = HandTypedExpense(repository);
        var lines = FundingLines(repository, ("Chase", 900m));

        var result = await service.UpdateIncomeAsync(older.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { NetValue = 1000m, Bank = null, FundingLines = lines }));

        using (new AssertionScope())
        {
            repository.Expenses.Should().ContainSingle().Which.Should().BeSameAs(handTyped);
            repository.PotTransactions.Should().HaveCount(4).And.OnlyContain(m => m.OperationId == null && m.Income!.Id == older.Id);
            repository.PotTransactions.Sum(m => m.Amount).Should().Be(900m);
            result.HasLinkedDeposit.Should().BeFalse();
            result.FundingLines.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task UpdateIncomeAsync_ForAnOlderSplitWhoseActivePotsDoNotTotal100_KeepsRebuildingWithoutTheRule()
    {
        var (repository, older) = RepositoryWithOlderSplitIncome();
        repository.ReservaPots.Single(p => p.Name == "Gleison").Update("Gleison", 0m, isActive: false);
        var service = CreateService(repository);

        await service.UpdateIncomeAsync(older.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { NetValue = 1000m }));

        repository.PotTransactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateIncomeAsync_TogglingAnOlderSplitOff_RemovesItsMovements()
    {
        var (repository, older) = RepositoryWithOlderSplitIncome();
        var service = CreateService(repository);
        var handTyped = HandTypedExpense(repository);

        await service.UpdateIncomeAsync(older.Id, ToUpdateDto(repository, ValidArianaSplitRequest() with { PotsAccount = null }));

        using (new AssertionScope())
        {
            repository.PotTransactions.Should().BeEmpty();
            repository.Expenses.Should().ContainSingle().Which.Should().BeSameAs(handTyped);
        }
    }

    [Fact]
    public async Task DeleteIncomeAsync_ForAnOlderSplit_RemovesItsMovementsAndKeepsTheHandTypedExpense()
    {
        var (repository, older) = RepositoryWithOlderSplitIncome();
        var service = CreateService(repository);
        var handTyped = HandTypedExpense(repository);

        await service.DeleteIncomeAsync(older.Id);

        using (new AssertionScope())
        {
            repository.Incomes.Should().BeEmpty();
            repository.PotTransactions.Should().BeEmpty();
            repository.Expenses.Should().ContainSingle().Which.Should().BeSameAs(handTyped);
        }
    }

    [Fact]
    public void GetIncomesByMonth_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetIncomesByMonth(2026, 9);

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.IncomeService.GetIncomesByMonth");
    }
}
