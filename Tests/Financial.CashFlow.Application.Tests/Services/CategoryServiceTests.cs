using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CategoryServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<CategoryService> Logger = NullLogger<CategoryService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultCategories: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository does not repeat the whole construction sequence.</summary>
    private CategoryService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CategoryService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CategoryService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetCategories_MapsEveryRepositoryCategoryToADto()
    {
        var result = _sut.GetCategories();

        result.Should().HaveCount(_repository.Categories.Count);
        result.Should().Contain(c => c.Name == "Mercado" && !c.IsInvestment && !c.IsTithe);
    }

    [Fact]
    public void GetCategories_RecordsSuccessfulSpan()
    {
        _sut.GetCategories();

        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("CashFlow.CategoryService.GetCategories");
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("CashFlow");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("Category");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new CategoryService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
