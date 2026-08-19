using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class IncomeSourceServiceTests
{
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();
    private static readonly Microsoft.Extensions.Logging.ILogger<IncomeSourceService> Logger = NullLogger<IncomeSourceService>.Instance;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new IncomeSourceService(null!, Tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new IncomeSourceService(new StubCashFlowRepository(), null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetIncomeSources_MapsEveryRepositoryIncomeSourceToADto()
    {
        var repository = new StubCashFlowRepository();
        var gleison = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        var lottery = IncomeSource.Create("Lottery", IncomeGroup.NonReportable, isActive: false);
        repository.IncomeSources.Add(gleison);
        repository.IncomeSources.Add(lottery);
        var service = new IncomeSourceService(repository, Tracer, Logger);

        var result = service.GetIncomeSources();

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
        var repository = new StubCashFlowRepository();
        repository.IncomeSources.Add(IncomeSource.Create("RetiredSource", IncomeGroup.NonReportable, isActive: false));
        var service = new IncomeSourceService(repository, Tracer, Logger);

        var result = service.GetIncomeSources();

        result.Should().ContainSingle(s => s.Name == "RetiredSource" && !s.IsActive);
    }

    [Fact]
    public void GetIncomeSources_WithNoIncomeSources_ReturnsEmptyList()
    {
        var service = new IncomeSourceService(new StubCashFlowRepository(), Tracer, Logger);

        var result = service.GetIncomeSources();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new IncomeSourceService(new StubCashFlowRepository(), Tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
