using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
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

    [Fact]
    public async Task CreateReserveBucketAsync_WithValidRequest_AddsAndSaves()
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketCreateDTO { Name = "Ferias", SplitPercentage = 100m, IsActive = true };

        var result = await service.CreateReserveBucketAsync(request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Ferias");
            result.SplitPercentage.Should().Be(100m);
            result.Warning.Should().BeNull();
            repository.ReserveBuckets.Should().ContainSingle(b => b.Name == "Ferias");
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateReserveBucketAsync_WithoutAName_ThrowsAndWritesNothing(string? name)
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketCreateDTO { Name = name!, SplitPercentage = 50m, IsActive = true };

        var act = async () => await service.CreateReserveBucketAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateReserveBucketAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        var repository = new StubCashFlowRepository();
        repository.ReserveBuckets.Add(ReserveBucket.Create("Investimento", 33.33m));
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketCreateDTO { Name = "Investimento", SplitPercentage = 50m, IsActive = true };

        var act = async () => await service.CreateReserveBucketAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateReserveBucketAsync_WithSplitPercentageOutOfRange_ThrowsAndWritesNothing()
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketCreateDTO { Name = "Ferias", SplitPercentage = 100.01m, IsActive = true };

        var act = async () => await service.CreateReserveBucketAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateReserveBucketAsync_WhenActiveSplitsDoNotSumTo100_ReturnsDtoWithWarning()
    {
        var repository = new StubCashFlowRepository();
        repository.ReserveBuckets.Add(ReserveBucket.Create("Investimento", 50m));
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketCreateDTO { Name = "Ferias", SplitPercentage = 20m, IsActive = true };

        var result = await service.CreateReserveBucketAsync(request);

        result.Warning.Should().Contain("70").And.Contain("review your split percentages");
    }

    [Fact]
    public async Task CreateReserveBucketAsync_WhenActiveSplitsSumTo100_ReturnsDtoWithNullWarning()
    {
        var repository = new StubCashFlowRepository();
        repository.ReserveBuckets.Add(ReserveBucket.Create("Investimento", 60m));
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketCreateDTO { Name = "Ferias", SplitPercentage = 40m, IsActive = true };

        var result = await service.CreateReserveBucketAsync(request);

        result.Warning.Should().BeNull();
    }

    [Fact]
    public async Task CreateReserveBucketAsync_IgnoresInactiveBucketsInTheSplitTotal()
    {
        var repository = new StubCashFlowRepository();
        repository.ReserveBuckets.Add(ReserveBucket.Create("Retired", 50m, isActive: false));
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketCreateDTO { Name = "Ferias", SplitPercentage = 100m, IsActive = true };

        var result = await service.CreateReserveBucketAsync(request);

        result.Warning.Should().BeNull();
    }

    [Fact]
    public async Task UpdateReserveBucketAsync_WithValidRequest_UpdatesAndSaves()
    {
        var repository = new StubCashFlowRepository();
        var bucket = ReserveBucket.Create("Investimento", 33.33m, isActive: true);
        repository.ReserveBuckets.Add(bucket);
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketUpdateDTO { Name = "Ferias", SplitPercentage = 100m, IsActive = false };

        var result = await service.UpdateReserveBucketAsync(bucket.Id, request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Ferias");
            result.SplitPercentage.Should().Be(100m);
            result.IsActive.Should().BeFalse();
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateReserveBucketAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketUpdateDTO { Name = "Ferias", SplitPercentage = 50m, IsActive = true };

        var act = async () => await service.UpdateReserveBucketAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateReserveBucketAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        var repository = new StubCashFlowRepository();
        var investimento = ReserveBucket.Create("Investimento", 33.33m);
        var ferias = ReserveBucket.Create("Ferias", 16.67m);
        repository.ReserveBuckets.Add(investimento);
        repository.ReserveBuckets.Add(ferias);
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketUpdateDTO { Name = "Ferias", SplitPercentage = 50m, IsActive = true };

        var act = async () => await service.UpdateReserveBucketAsync(investimento.Id, request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateReserveBucketAsync_DeactivatingTheBucket_ExcludesItFromTheSplitTotal()
    {
        var repository = new StubCashFlowRepository();
        var toDeactivate = ReserveBucket.Create("Investimento", 50m, isActive: true);
        repository.ReserveBuckets.Add(toDeactivate);
        repository.ReserveBuckets.Add(ReserveBucket.Create("Ferias", 50m, isActive: true));
        var service = new ReserveBucketService(repository, Tracer, Logger);
        var request = new ReserveBucketUpdateDTO { Name = "Investimento", SplitPercentage = 50m, IsActive = false };

        var result = await service.UpdateReserveBucketAsync(toDeactivate.Id, request);

        using (new AssertionScope())
        {
            result.IsActive.Should().BeFalse();
            result.Warning.Should().Contain("50");
        }
    }
}
