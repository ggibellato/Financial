using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class ReserveServiceTests
{
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();
    private static readonly Microsoft.Extensions.Logging.ILogger<ReserveService> Logger = NullLogger<ReserveService>.Instance;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ReserveService(null!, Tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new ReserveService(new StubCashFlowRepository(), null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithValidRequest_PostsOneMovementPerActiveBucketAndReturnsAmounts()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        var service = new ReserveService(repository, Tracer, Logger);

        var result = await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        using (new AssertionScope())
        {
            repository.ReserveMovements.Should().HaveCount(4);
            repository.ReserveMovements.Should().OnlyContain(m => m.Description == "Ramsay");
            result.Buckets.Should().HaveCount(4);
            result.Buckets.Should().ContainSingle(b => b.Bucket == "Investimento" && b.Amount == 654.27m);
            result.Buckets.Should().ContainSingle(b => b.Bucket == "HouseTreats" && b.Amount == 654.27m);
            result.Buckets.Should().ContainSingle(b => b.Bucket == "Ariana" && b.Amount == 327.23m);
            result.Buckets.Should().ContainSingle(b => b.Bucket == "Gleison" && b.Amount == 327.23m);
            result.Total.Should().Be(1963.00m);
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithInactiveBucket_ExcludesItFromMovementsAndResult()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.ReserveBuckets.RemoveAll(b => b.Name == "Gleison");
        repository.ReserveBuckets.Add(ReserveBucket.Create("Gleison", 16.67m, isActive: false));
        var service = new ReserveService(repository, Tracer, Logger);

        var result = await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        using (new AssertionScope())
        {
            repository.ReserveMovements.Should().HaveCount(3);
            result.Buckets.Should().HaveCount(3);
            result.Buckets.Should().NotContain(b => b.Bucket == "Gleison");
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithNoActiveBuckets_ThrowsArgumentExceptionBeforeTouchingRepository()
    {
        var repository = new StubCashFlowRepository();
        repository.ReserveBuckets.Add(ReserveBucket.Create("Investimento", 33.33m, isActive: false));
        var service = new ReserveService(repository, Tracer, Logger);

        var act = async () => await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        await act.Should().ThrowAsync<ArgumentException>();
        repository.ReserveMovements.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostIncomeSplitAsync_WithNonPositiveAmount_ThrowsBeforeTouchingRepository(decimal amount)
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveService(repository, Tracer, Logger);
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = amount,
            Description = "Ramsay"
        };

        var act = async () => await service.PostIncomeSplitAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        repository.ReserveMovements.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostIncomeSplitAsync_WithMissingDescription_ThrowsBeforeTouchingRepository(string? description)
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveService(repository, Tracer, Logger);
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = description!
        };

        var act = async () => await service.PostIncomeSplitAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        repository.ReserveMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WhenSaveFails_RollsBackAllFourMovements()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true) { ThrowOnNextSave = true };
        var service = new ReserveService(repository, Tracer, Logger);

        var act = async () => await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        repository.ReserveMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithinBalance_PostsNegativeMovement()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.Seed("Investimento", 100m);
        var service = new ReserveService(repository, Tracer, Logger);

        var result = await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "Investimento",
            Amount = 30m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Groceries top-up",
            Confirmed = false
        });

        result.Amount.Should().Be(-30m);
        repository.ReserveMovements.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostWithdrawalAsync_ExceedingBalanceUnconfirmed_ThrowsOverdraftException()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.Seed("Ariana", 50m);
        var service = new ReserveService(repository, Tracer, Logger);

        var act = async () => await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "Ariana",
            Amount = 100m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Big purchase",
            Confirmed = false
        });

        await act.Should().ThrowAsync<OverdraftConfirmationRequiredException>().WithMessage("*Ariana*50*");
        repository.ReserveMovements.Should().HaveCount(1);
    }

    [Fact]
    public async Task PostWithdrawalAsync_ExceedingBalanceConfirmed_Saves()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.Seed("Ariana", 50m);
        var service = new ReserveService(repository, Tracer, Logger);

        var result = await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "Ariana",
            Amount = 100m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Big purchase",
            Confirmed = true
        });

        result.Amount.Should().Be(-100m);
        repository.ReserveMovements.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithZeroAmount_ThrowsArgumentException()
    {
        var service = new ReserveService(new StubCashFlowRepository(), Tracer, Logger);

        var act = async () => await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "Investimento",
            Amount = 0m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Nothing"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithUnknownBucket_ThrowsArgumentException()
    {
        var service = new ReserveService(new StubCashFlowRepository(seedDefaultReserveBuckets: true), Tracer, Logger);

        var act = async () => await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "NotABucket",
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not recognized*");
    }

    [Fact]
    public void GetBucketBalances_AlwaysReturnsExactlyFourBuckets()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        var service = new ReserveService(repository, Tracer, Logger);

        var balances = service.GetBucketBalances();

        balances.Should().HaveCount(4);
        balances.Should().OnlyContain(b => b.Balance == 0m);
    }

    [Fact]
    public async Task GetBucketBalances_ReflectsPostedMovements()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        var service = new ReserveService(repository, Tracer, Logger);
        await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        var balances = service.GetBucketBalances();

        balances.Should().ContainSingle(b => b.Bucket == "Investimento" && b.Balance == 654.27m);
    }

    [Fact]
    public void GetBucketBalances_IncludesInactiveBucketsWithTheirBalance()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.ReserveBuckets.RemoveAll(b => b.Name == "Gleison");
        var retiredGleison = ReserveBucket.Create("Gleison", 16.67m, isActive: false);
        repository.ReserveBuckets.Add(retiredGleison);
        repository.ReserveMovements.Add(ReserveMovement.Create(retiredGleison, 75m, new DateOnly(2026, 7, 1), "Before retirement"));
        var service = new ReserveService(repository, Tracer, Logger);

        var balances = service.GetBucketBalances();

        balances.Should().HaveCount(4);
        balances.Should().ContainSingle(b => b.Bucket == "Gleison" && b.Balance == 75m);
    }

    [Fact]
    public void GetBucketBalances_IsNotHardcodedToFourBuckets()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.ReserveBuckets.Add(ReserveBucket.Create("Emergency", 0m, isActive: false));
        var service = new ReserveService(repository, Tracer, Logger);

        var balances = service.GetBucketBalances();

        balances.Should().HaveCount(5);
    }

    [Fact]
    public void GetMovementHistory_ReturnsAllMovementsOrderedByDateDescending()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.Seed("Investimento", 10m, new DateOnly(2026, 8, 1));
        repository.Seed("Investimento", 5m, new DateOnly(2026, 7, 1));
        var service = new ReserveService(repository, Tracer, Logger);

        var history = service.GetMovementHistory();

        history.Should().HaveCount(2);
        history.Select(m => m.Date).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task UpdateMovementAsync_ExistingId_UpdatesFieldsAndSaves()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.Seed("Investimento", 100m, new DateOnly(2026, 7, 1));
        var movement = repository.ReserveMovements[0];
        var service = new ReserveService(repository, Tracer, Logger);

        var result = await service.UpdateMovementAsync(movement.Id, new UpdateReserveMovementDTO
        {
            Bucket = "HouseTreats",
            Amount = 150m,
            Date = new DateOnly(2026, 7, 5),
            Description = "Corrected"
        });

        using (new AssertionScope())
        {
            result.Bucket.Should().Be("HouseTreats");
            result.Amount.Should().Be(150m);
            result.Date.Should().Be(new DateOnly(2026, 7, 5));
            result.Description.Should().Be("Corrected");
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateMovementAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new ReserveService(new StubCashFlowRepository(seedDefaultReserveBuckets: true), Tracer, Logger);

        var act = async () => await service.UpdateMovementAsync(Guid.NewGuid(), new UpdateReserveMovementDTO
        {
            Bucket = "Investimento",
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateMovementAsync_WithUnknownBucket_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.Seed("Investimento", 100m);
        var service = new ReserveService(repository, Tracer, Logger);

        var act = async () => await service.UpdateMovementAsync(repository.ReserveMovements[0].Id, new UpdateReserveMovementDTO
        {
            Bucket = "NotABucket",
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not recognized*");
    }

    [Fact]
    public async Task DeleteMovementAsync_SoloMovement_DeletesOnlyThatOne()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        repository.Seed("Investimento", -30m, new DateOnly(2026, 7, 1));
        var toDelete = repository.ReserveMovements[0];
        repository.Seed("Ariana", -20m, new DateOnly(2026, 7, 2));
        var service = new ReserveService(repository, Tracer, Logger);

        await service.DeleteMovementAsync(toDelete.Id);

        repository.ReserveMovements.Should().ContainSingle();
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteMovementAsync_MovementFromASplit_DeletesAllFourSiblingMovements()
    {
        var repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        var service = new ReserveService(repository, Tracer, Logger);
        await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());
        var oneLineOfTheSplit = repository.ReserveMovements[0];

        await service.DeleteMovementAsync(oneLineOfTheSplit.Id);

        repository.ReserveMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteMovementAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new ReserveService(new StubCashFlowRepository(), Tracer, Logger);

        var act = async () => await service.DeleteMovementAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static IncomeSplitRequestDTO ValidIncomeSplitRequest() => new()
    {
        Date = new DateOnly(2026, 7, 1),
        Amount = 1963m,
        Description = "Ramsay"
    };

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new ReserveService(new StubCashFlowRepository(), Tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }

}

internal static class ReserveServiceTestsStubExtensions
{
    public static void Seed(this StubCashFlowRepository repository, string bucketName, decimal amount, DateOnly? date = null)
    {
        var bucket = repository.ReserveBuckets.First(b => b.Name == bucketName);
        repository.ReserveMovements.Add(ReserveMovement.Create(bucket, amount, date ?? new DateOnly(2026, 1, 1), "Seed"));
    }

}
