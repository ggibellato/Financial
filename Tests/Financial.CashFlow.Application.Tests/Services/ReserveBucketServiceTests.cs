using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class ReserveBucketServiceTests
{
    private readonly RecordingTelemetryTracer _tracer = new();
    private static readonly Microsoft.Extensions.Logging.ILogger<ReserveBucketService> Logger = NullLogger<ReserveBucketService>.Instance;

    [Fact]
    public void GetReserveBuckets_MapsEveryRepositoryBucketToADto()
    {
        var repository = new StubCashFlowRepository();
        var investimento = Pot.Create("Investimento", 33.33m);
        var ariana = Pot.Create("Ariana", 16.67m, isActive: false);
        repository.AddPot(investimento);
        repository.AddPot(ariana);
        var service = new ReserveBucketService(repository, _tracer, Logger);

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
    public void GetSplitStatus_NoBuckets_HasNoWarning()
    {
        var service = new ReserveBucketService(new StubCashFlowRepository(), _tracer, Logger);

        var result = service.GetSplitStatus();

        using (new AssertionScope())
        {
            result.ActiveTotal.Should().Be(0m);
            result.Warning.Should().BeNull();
        }
    }

    [Fact]
    public void GetSplitStatus_ActiveBucketsSumTo100_HasNoWarning()
    {
        var repository = new StubCashFlowRepository();
        repository.AddPot(Pot.Create("A", 60m));
        repository.AddPot(Pot.Create("B", 40m));
        var service = new ReserveBucketService(repository, _tracer, Logger);

        var result = service.GetSplitStatus();

        using (new AssertionScope())
        {
            result.ActiveTotal.Should().Be(100m);
            result.Warning.Should().BeNull();
        }
    }

    [Fact]
    public void GetSplitStatus_ActiveBucketsDoNotSumTo100_NamesTheTotal_AndIgnoresInactiveBuckets()
    {
        var repository = new StubCashFlowRepository();
        repository.AddPot(Pot.Create("A", 60m));
        repository.AddPot(Pot.Create("B", 30m));
        repository.AddPot(Pot.Create("Retired", 10m, isActive: false));
        var service = new ReserveBucketService(repository, _tracer, Logger);

        var result = service.GetSplitStatus();

        using (new AssertionScope())
        {
            result.ActiveTotal.Should().Be(90m);
            result.Warning.Should().Be("Active buckets currently sum to 90% — review your split percentages");
        }
    }

    [Fact]
    public void GetReserveBuckets_DoesNotFilterByIsActive()
    {
        var repository = new StubCashFlowRepository();
        repository.AddPot(Pot.Create("Retired", 0m, isActive: false));
        var service = new ReserveBucketService(repository, _tracer, Logger);

        var result = service.GetReserveBuckets();

        result.Should().ContainSingle(b => b.Name == "Retired" && !b.IsActive);
    }

    [Fact]
    public void GetReserveBuckets_WithNoBuckets_ReturnsEmptyList()
    {
        var service = new ReserveBucketService(new StubCashFlowRepository(), _tracer, Logger);

        var result = service.GetReserveBuckets();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetReserveBuckets_SecondPotsAccount_ListsOnlyReservasPots()
    {
        var repository = new StubCashFlowRepository();
        repository.AddPot(Pot.Create("Investimento", 100m));
        var holiday = TestAccounts.Pots("Holiday");
        holiday.AddPot(Pot.Create("Trip", 100m));
        repository.Accounts.Add(holiday);
        var service = new ReserveBucketService(repository, _tracer, Logger);

        service.GetReserveBuckets().Select(b => b.Name).Should().Equal("Investimento");
    }

    [Fact]
    public void GetReserveBuckets_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        var repository = new StubCashFlowRepository { ThrowOnNextRead = new InvalidOperationException("simulated failure") };
        var service = new ReserveBucketService(repository, _tracer, Logger);

        Action act = () => service.GetReserveBuckets();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.ReserveBucketService.GetReserveBuckets");
    }
}
