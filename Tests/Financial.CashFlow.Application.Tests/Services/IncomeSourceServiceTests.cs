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
}
