using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class InvestmentSnapshotServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly int PastYear = CurrentYear - 5;
    private static readonly Microsoft.Extensions.Logging.ILogger<InvestmentSnapshotService> Logger = NullLogger<InvestmentSnapshotService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly InvestmentSnapshotService _sut;

    public InvestmentSnapshotServiceTests()
    {
        _repository = CreateRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private InvestmentSnapshotService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new InvestmentSnapshotService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new InvestmentSnapshotService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_FirstCall_GeneratesExactlyElevenSnapshotsDefaultingToZero()
    {
        var result = await _sut.GetSnapshotsForMonthAsync(CurrentYear, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(11);
            result.Should().OnlyContain(s => s.Value == 0m);
            _repository.InvestmentSnapshots.Should().HaveCount(11);
        }
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_MarksTheSixLiabilityAccountsCorrectly()
    {
        var result = await _sut.GetSnapshotsForMonthAsync(CurrentYear, 7);

        using (new AssertionScope())
        {
            result.Where(s => s.IsLiability).Should().HaveCount(6);
            result.Should().ContainSingle(s => s.AccountName == "PlatinumVisa8003" && s.IsLiability);
            result.Should().ContainSingle(s => s.AccountName == "ReservasPessoais" && s.IsLiability);
            result.Should().ContainSingle(s => s.AccountName == "ChaseSave" && !s.IsLiability);
        }
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_SecondCallSameMonth_DoesNotCreateDuplicates()
    {
        await _sut.GetSnapshotsForMonthAsync(CurrentYear, 7);
        var result = await _sut.GetSnapshotsForMonthAsync(CurrentYear, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(11);
            _repository.InvestmentSnapshots.Should().HaveCount(11);
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_CurrentYear_ExcludesDisabledAccounts()
    {
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false));

        var result = await _sut.GetSnapshotsForMonthAsync(CurrentYear, 7);

        result.Should().HaveCount(11);
        result.Should().NotContain(s => s.AccountName == "EverydaySaver");
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_PastYearWithNoExistingData_ReturnsEmptyNotAllAccounts()
    {
        var result = await _sut.GetSnapshotsForMonthAsync(PastYear, 7);

        result.Should().BeEmpty();
        _repository.InvestmentSnapshots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_PastYearWithSomeAccountsPresent_ReturnsOnlyThose()
    {
        var chaseSave = _repository.InvestmentAccounts.First(a => a.Name == "ChaseSave");
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(chaseSave, PastYear, 7, 100m));

        var result = await _sut.GetSnapshotsForMonthAsync(PastYear, 7);

        result.Should().ContainSingle().Which.AccountName.Should().Be("ChaseSave");
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_UpdatesOnlyTheTargetedSnapshot()
    {
        await _sut.GetSnapshotsForMonthAsync(CurrentYear, 7);
        await _sut.GetSnapshotsForMonthAsync(CurrentYear, 8);
        var julySnapshot = _repository.InvestmentSnapshots.Single(s => s.Month == 7 && s.Account.Name == "ChaseSave");
        var augustSnapshot = _repository.InvestmentSnapshots.Single(s => s.Month == 8 && s.Account.Name == "ChaseSave");
        var otherAccountSnapshot = _repository.InvestmentSnapshots.Single(s => s.Month == 7 && s.Account.Name == "PlatinumVisa8003");

        var result = await _sut.UpdateSnapshotValueAsync(julySnapshot.Id, new InvestmentSnapshotValueUpdateDTO { Value = 500m });

        using (new AssertionScope())
        {
            result.Value.Should().Be(500m);
            augustSnapshot.Value.Should().Be(0m);
            otherAccountSnapshot.Value.Should().Be(0m);
        }
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_WithNegativeValue_ThrowsArgumentException()
    {
        await _sut.GetSnapshotsForMonthAsync(CurrentYear, 7);
        var snapshot = _repository.InvestmentSnapshots.First();

        var act = async () => await _sut.UpdateSnapshotValueAsync(snapshot.Id, new InvestmentSnapshotValueUpdateDTO { Value = -1m });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateSnapshotValueAsync(Guid.NewGuid(), new InvestmentSnapshotValueUpdateDTO { Value = 10m });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static StubCashFlowRepository CreateRepository()
    {
        var repository = new StubCashFlowRepository();
        SeededInvestmentAccounts.SeedInto(repository);
        return repository;
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new InvestmentSnapshotService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
