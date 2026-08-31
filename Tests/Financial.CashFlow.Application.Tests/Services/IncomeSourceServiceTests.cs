using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class IncomeSourceServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<IncomeSourceService> Logger = NullLogger<IncomeSourceService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly IncomeSourceService _sut;

    public IncomeSourceServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private IncomeSourceService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new IncomeSourceService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new IncomeSourceService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetIncomeSources_MapsEveryRepositoryIncomeSourceToADto()
    {
        var gleison = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        var lottery = IncomeSource.Create("Lottery", IncomeGroup.NonReportable, isActive: false);
        _repository.IncomeSources.Add(gleison);
        _repository.IncomeSources.Add(lottery);

        var result = _sut.GetIncomeSources();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            var gleisonDto = result.Should().ContainSingle(s => s.Name == "Gleison").Which;
            gleisonDto.Id.Should().Be(gleison.Id);
            gleisonDto.IsActive.Should().BeTrue();
            gleisonDto.Group.Should().Be("Salary");
        }
    }

    [Fact]
    public void GetIncomeSources_DoesNotFilterByIsActive()
    {
        _repository.IncomeSources.Add(IncomeSource.Create("RetiredSource", IncomeGroup.NonReportable, isActive: false));

        var result = _sut.GetIncomeSources();

        result.Should().ContainSingle(s => s.Name == "RetiredSource" && !s.IsActive);
    }

    [Fact]
    public void GetIncomeSources_WithNoIncomeSources_ReturnsEmptyList()
    {
        var result = _sut.GetIncomeSources();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetIncomeSources_ReturnsAutoSplitToReserveField()
    {
        var ariana = IncomeSource.Create("Ariana", IncomeGroup.Salary, autoSplitToReserve: true);
        var gleison = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        _repository.IncomeSources.Add(ariana);
        _repository.IncomeSources.Add(gleison);

        var result = _sut.GetIncomeSources();

        using (new AssertionScope())
        {
            result.Should().ContainSingle(s => s.Name == "Ariana").Which.AutoSplitToReserve.Should().BeTrue();
            result.Should().ContainSingle(s => s.Name == "Gleison").Which.AutoSplitToReserve.Should().BeFalse();
        }
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new IncomeSourceService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateIncomeSourceAsync_WithValidRequest_AddsAndSaves()
    {
        var request = new IncomeSourceCreateDTO { Name = "Freelance", Group = "NonReportable", IsActive = true, AutoSplitToReserve = false };

        var result = await _sut.CreateIncomeSourceAsync(request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Freelance");
            result.Group.Should().Be("NonReportable");
            result.HasReferences.Should().BeFalse();
            _repository.IncomeSources.Should().ContainSingle(s => s.Name == "Freelance");
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateIncomeSourceAsync_WithoutAName_ThrowsAndWritesNothing(string? name)
    {
        var request = new IncomeSourceCreateDTO { Name = name!, Group = "Salary", IsActive = true, AutoSplitToReserve = false };

        var act = async () => await _sut.CreateIncomeSourceAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateIncomeSourceAsync_WithInvalidGroup_ThrowsAndWritesNothing()
    {
        var request = new IncomeSourceCreateDTO { Name = "Freelance", Group = "NotAGroup", IsActive = true, AutoSplitToReserve = false };

        var act = async () => await _sut.CreateIncomeSourceAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateIncomeSourceAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        _repository.IncomeSources.Add(IncomeSource.Create("Gleison", IncomeGroup.Salary));
        var request = new IncomeSourceCreateDTO { Name = "Gleison", Group = "Salary", IsActive = true, AutoSplitToReserve = false };

        var act = async () => await _sut.CreateIncomeSourceAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateIncomeSourceAsync_WithValidRequest_UpdatesAndSaves()
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        _repository.IncomeSources.Add(incomeSource);
        var request = new IncomeSourceUpdateDTO { Name = "Ariana", Group = "NonReportable", IsActive = false, AutoSplitToReserve = true };

        var result = await _sut.UpdateIncomeSourceAsync(incomeSource.Id, request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Ariana");
            result.Group.Should().Be("NonReportable");
            result.IsActive.Should().BeFalse();
            result.AutoSplitToReserve.Should().BeTrue();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateIncomeSourceAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var request = new IncomeSourceUpdateDTO { Name = "Ariana", Group = "Salary", IsActive = true, AutoSplitToReserve = false };

        var act = async () => await _sut.UpdateIncomeSourceAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateIncomeSourceAsync_WithInvalidGroup_ThrowsAndWritesNothing()
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        _repository.IncomeSources.Add(incomeSource);
        var request = new IncomeSourceUpdateDTO { Name = "Gleison", Group = "NotAGroup", IsActive = true, AutoSplitToReserve = false };

        var act = async () => await _sut.UpdateIncomeSourceAsync(incomeSource.Id, request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateIncomeSourceAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        var gleison = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        var ariana = IncomeSource.Create("Ariana", IncomeGroup.Salary);
        _repository.IncomeSources.Add(gleison);
        _repository.IncomeSources.Add(ariana);
        var request = new IncomeSourceUpdateDTO { Name = "Ariana", Group = "Salary", IsActive = true, AutoSplitToReserve = false };

        var act = async () => await _sut.UpdateIncomeSourceAsync(gleison.Id, request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteIncomeSourceAsync_WithNoReferences_RemovesAndSaves()
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        _repository.IncomeSources.Add(incomeSource);

        await _sut.DeleteIncomeSourceAsync(incomeSource.Id);

        using (new AssertionScope())
        {
            _repository.IncomeSources.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteIncomeSourceAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteIncomeSourceAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteIncomeSourceAsync_ReferencedByIncome_ThrowsAndWritesNothing()
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        _repository.IncomeSources.Add(incomeSource);
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), incomeSource, null, 500m, bank));

        var act = async () => await _sut.DeleteIncomeSourceAsync(incomeSource.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>();
            _repository.IncomeSources.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public void GetIncomeSources_HasReferences_ReflectsWhetherAnIncomeExists()
    {
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        _repository.IncomeSources.Add(incomeSource);
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        _repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), incomeSource, null, 500m, bank));

        var result = _sut.GetIncomeSources();

        result.Should().ContainSingle(s => s.Id == incomeSource.Id).Which.HasReferences.Should().BeTrue();
    }
}
