using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CategoryServiceTests
{
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CategoryService(null!, Tracer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CategoryService(new StubCashFlowRepository(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetCategories_MapsEveryRepositoryCategoryToADto()
    {
        var repository = new StubCashFlowRepository(seedDefaultCategories: true);
        var service = new CategoryService(repository, Tracer);

        var result = service.GetCategories();

        result.Should().HaveCount(repository.Categories.Count);
        result.Should().Contain(c => c.Name == "Mercado" && !c.IsInvestment && !c.IsTithe);
    }

    [Fact]
    public void GetCategories_RecordsSuccessfulSpan()
    {
        var repository = new StubCashFlowRepository(seedDefaultCategories: true);
        var tracer = new RecordingTelemetryTracer();
        var service = new CategoryService(repository, tracer);

        service.GetCategories();

        var span = tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("CashFlow.CategoryService.GetCategories");
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("CashFlow");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("Category");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }
}
