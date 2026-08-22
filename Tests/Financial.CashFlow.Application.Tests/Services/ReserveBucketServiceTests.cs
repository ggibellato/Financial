using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class ReserveBucketServiceTests
{
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();
    private static readonly Microsoft.Extensions.Logging.ILogger<ReserveBucketService> Logger = NullLogger<ReserveBucketService>.Instance;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ReserveBucketService(null!, Tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new ReserveBucketService(new StubCashFlowRepository(), null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetReserveBuckets_MapsEveryRepositoryBucketToADto()
    {
        var repository = new StubCashFlowRepository();
        var investimento = ReserveBucket.Create("Investimento", 33.33m);
        var ariana = ReserveBucket.Create("Ariana", 16.67m, isActive: false);
        repository.ReserveBuckets.Add(investimento);
        repository.ReserveBuckets.Add(ariana);
        var service = new ReserveBucketService(repository, Tracer, Logger);

        var result = service.GetReserveBuckets();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            var investimentoDto = result.Should().ContainSingle(b => b.Name == "Investimento").Which;
            investimentoDto.Id.Should().Be(investimento.Id);
            investimentoDto.IsActive.Should().BeTrue();
            investimentoDto.SplitPercentage.Should().Be(33.33m);
        }
    }

    [Fact]
    public void GetReserveBuckets_DoesNotFilterByIsActive()
    {
        var repository = new StubCashFlowRepository();
        repository.ReserveBuckets.Add(ReserveBucket.Create("Retired", 0m, isActive: false));
        var service = new ReserveBucketService(repository, Tracer, Logger);

        var result = service.GetReserveBuckets();

        result.Should().ContainSingle(b => b.Name == "Retired" && !b.IsActive);
    }

    [Fact]
    public void GetReserveBuckets_WithNoBuckets_ReturnsEmptyList()
    {
        var service = new ReserveBucketService(new StubCashFlowRepository(), Tracer, Logger);

        var result = service.GetReserveBuckets();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new ReserveBucketService(new StubCashFlowRepository(), Tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
