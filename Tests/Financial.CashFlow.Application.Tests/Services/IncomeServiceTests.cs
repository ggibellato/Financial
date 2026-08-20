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

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository does not repeat the whole construction sequence.</summary>
    private IncomeService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new IncomeService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new IncomeService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

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
        Description = r.Description
    };

    private static IncomeUpdateDTO ToUpdateDto(StubCashFlowRepository repository, IncomeCreateRequest r) => new()
    {
        Date = r.Date,
        IncomeSourceId = ResolveIncomeSourceId(repository, r.IncomeSource),
        GrossValue = r.GrossValue,
        NetValue = r.NetValue,
        BankId = ResolveBankId(repository, r.Bank),
        Description = r.Description
    };

    /// <summary>An unresolvable name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path.</summary>
    private static Guid ResolveIncomeSourceId(StubCashFlowRepository repository, string? incomeSourceName) =>
        repository.IncomeSources.FirstOrDefault(s => s.Name == incomeSourceName)?.Id ?? Guid.NewGuid();

    /// <summary>Null bank name means "no bank supplied"; an unresolvable non-null name maps to a random, never-seeded Guid so tests exercising an unrecognized reference still hit the "not found" path.</summary>
    private static Guid? ResolveBankId(StubCashFlowRepository repository, string? bankName) =>
        bankName is null ? null : repository.Banks.FirstOrDefault(b => b.Name == bankName)?.Id ?? Guid.NewGuid();

    private sealed record IncomeCreateRequest(
        DateOnly Date, string IncomeSource, decimal? GrossValue, decimal NetValue, string? Bank, string? Description);

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new IncomeService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
