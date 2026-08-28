using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class ReserveServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<ReserveService> Logger = NullLogger<ReserveService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly ReserveService _sut;

    public ReserveServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultReserveBuckets: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private ReserveService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    /// <summary>The seeded bucket's id. The stub repository generates a fresh Guid per test run,
    /// so a test names the bucket it means and lets this resolve the id it must send.</summary>
    private Guid BucketId(string name) => _repository.ReserveBuckets.First(b => b.Name == name).Id;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ReserveService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new ReserveService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithValidRequest_PostsOneMovementPerActiveBucketAndReturnsAmounts()
    {
        var result = await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        using (new AssertionScope())
        {
            _repository.ReserveMovements.Should().HaveCount(4);
            _repository.ReserveMovements.Should().OnlyContain(m => m.Description == "Ramsay");
            result.Buckets.Should().HaveCount(4);
            result.Buckets.Should().ContainSingle(b => b.BucketName == "Investimento" && b.Amount == 654.27m);
            result.Buckets.Should().ContainSingle(b => b.BucketName == "HouseTreats" && b.Amount == 654.27m);
            result.Buckets.Should().ContainSingle(b => b.BucketName == "Ariana" && b.Amount == 327.23m);
            result.Buckets.Should().ContainSingle(b => b.BucketName == "Gleison" && b.Amount == 327.23m);
            result.Total.Should().Be(1963.00m);
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithInactiveBucket_ExcludesItFromMovementsAndResult()
    {
        _repository.ReserveBuckets.RemoveAll(b => b.Name == "Gleison");
        _repository.ReserveBuckets.Add(ReserveBucket.Create("Gleison", 16.67m, isActive: false));

        var result = await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        using (new AssertionScope())
        {
            _repository.ReserveMovements.Should().HaveCount(3);
            result.Buckets.Should().HaveCount(3);
            result.Buckets.Should().NotContain(b => b.BucketName == "Gleison");
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithNoActiveBuckets_ThrowsArgumentExceptionBeforeTouchingRepository()
    {
        var repository = new StubCashFlowRepository();
        repository.ReserveBuckets.Add(ReserveBucket.Create("Investimento", 33.33m, isActive: false));
        var service = CreateService(repository);

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
        var service = CreateService(repository);
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
        var service = CreateService(repository);
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
        var service = CreateService(repository);

        var act = async () => await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        repository.ReserveMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithinBalance_PostsNegativeMovement()
    {
        _repository.Seed("Investimento", 100m);

        var result = await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 30m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Groceries top-up",
            Confirmed = false
        });

        result.Amount.Should().Be(-30m);
        _repository.ReserveMovements.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostWithdrawalAsync_ExceedingBalanceUnconfirmed_ThrowsOverdraftException()
    {
        _repository.Seed("Ariana", 50m);

        var act = async () => await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = BucketId("Ariana"),
            Amount = 100m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Big purchase",
            Confirmed = false
        });

        await act.Should().ThrowAsync<OverdraftConfirmationRequiredException>().WithMessage("*Ariana*50*");
        _repository.ReserveMovements.Should().HaveCount(1);
    }

    [Fact]
    public async Task PostWithdrawalAsync_ExceedingBalanceConfirmed_Saves()
    {
        _repository.Seed("Ariana", 50m);

        var result = await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = BucketId("Ariana"),
            Amount = 100m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Big purchase",
            Confirmed = true
        });

        result.Amount.Should().Be(-100m);
        _repository.ReserveMovements.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithZeroAmount_ThrowsArgumentException()
    {
        var service = CreateService(new StubCashFlowRepository());

        var act = async () => await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = Guid.NewGuid(),
            Amount = 0m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Nothing"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task PostWithdrawalAsync_TwoBucketsSharingAName_PostsAgainstTheOneWithTheGivenId()
    {
        var duplicate = ReserveBucket.Create("Investimento", 0m);
        _repository.ReserveBuckets.Add(duplicate);

        await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = duplicate.Id,
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test",
            Confirmed = true
        });

        _repository.ReserveMovements.Should().ContainSingle()
            .Which.Bucket.Should().BeSameAs(duplicate);
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithUnknownBucket_ThrowsArgumentException()
    {
        var act = async () => await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = Guid.NewGuid(),
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not recognized*");
    }

    [Fact]
    public void GetBucketBalances_AlwaysReturnsExactlyFourBuckets()
    {
        var balances = _sut.GetBucketBalances();

        balances.Should().HaveCount(4);
        balances.Should().OnlyContain(b => b.Balance == 0m);
    }

    [Fact]
    public async Task GetBucketBalances_ReflectsPostedMovements()
    {
        await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        var balances = _sut.GetBucketBalances();

        balances.Should().ContainSingle(b => b.BucketName == "Investimento" && b.Balance == 654.27m);
    }

    [Fact]
    public void GetBucketBalances_IncludesInactiveBucketsWithTheirBalance()
    {
        _repository.ReserveBuckets.RemoveAll(b => b.Name == "Gleison");
        var retiredGleison = ReserveBucket.Create("Gleison", 16.67m, isActive: false);
        _repository.ReserveBuckets.Add(retiredGleison);
        _repository.ReserveMovements.Add(ReserveMovement.Create(retiredGleison, 75m, new DateOnly(2026, 7, 1), "Before retirement"));

        var balances = _sut.GetBucketBalances();

        balances.Should().HaveCount(4);
        balances.Should().ContainSingle(b => b.BucketName == "Gleison" && b.Balance == 75m);
    }

    [Fact]
    public void GetBucketBalances_IsNotHardcodedToFourBuckets()
    {
        _repository.ReserveBuckets.Add(ReserveBucket.Create("Emergency", 0m, isActive: false));

        var balances = _sut.GetBucketBalances();

        balances.Should().HaveCount(5);
    }

    [Fact]
    public void GetMovementHistory_ReturnsAllMovementsOrderedByDateDescending()
    {
        _repository.Seed("Investimento", 10m, new DateOnly(2026, 8, 1));
        _repository.Seed("Investimento", 5m, new DateOnly(2026, 7, 1));

        var history = _sut.GetMovementHistory();

        history.Should().HaveCount(2);
        history.Select(m => m.Date).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task UpdateMovementAsync_ExistingId_UpdatesFieldsAndSaves()
    {
        _repository.Seed("Investimento", 100m, new DateOnly(2026, 7, 1));
        var movement = _repository.ReserveMovements[0];

        var result = await _sut.UpdateMovementAsync(movement.Id, new UpdateReserveMovementDTO
        {
            BucketId = BucketId("HouseTreats"),
            Amount = 150m,
            Date = new DateOnly(2026, 7, 5),
            Description = "Corrected"
        });

        using (new AssertionScope())
        {
            result.BucketId.Should().Be(BucketId("HouseTreats"));
            result.BucketName.Should().Be("HouseTreats");
            result.Amount.Should().Be(150m);
            result.Date.Should().Be(new DateOnly(2026, 7, 5));
            result.Description.Should().Be("Corrected");
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateMovementAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateMovementAsync(Guid.NewGuid(), new UpdateReserveMovementDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateMovementAsync_WithUnknownBucket_ThrowsArgumentException()
    {
        _repository.Seed("Investimento", 100m);

        var act = async () => await _sut.UpdateMovementAsync(_repository.ReserveMovements[0].Id, new UpdateReserveMovementDTO
        {
            BucketId = Guid.NewGuid(),
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not recognized*");
    }

    [Fact]
    public async Task DeleteMovementAsync_SoloMovement_DeletesOnlyThatOne()
    {
        _repository.Seed("Investimento", -30m, new DateOnly(2026, 7, 1));
        var toDelete = _repository.ReserveMovements[0];
        _repository.Seed("Ariana", -20m, new DateOnly(2026, 7, 2));

        await _sut.DeleteMovementAsync(toDelete.Id);

        _repository.ReserveMovements.Should().ContainSingle();
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteMovementAsync_MovementFromASplit_DeletesAllFourSiblingMovements()
    {
        await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());
        var oneLineOfTheSplit = _repository.ReserveMovements[0];

        await _sut.DeleteMovementAsync(oneLineOfTheSplit.Id);

        _repository.ReserveMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteMovementAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = CreateService(new StubCashFlowRepository());

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
        Action act = () => new ReserveService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static Income LinkedIncome() =>
        Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Create("Ariana", IncomeGroup.Salary, autoSplitToReserve: true), null, 2450m, null, "August salary", splitToReserve: true);

    [Fact]
    public async Task UpdateMovementAsync_OnLinkedMovement_ThrowsReserveMovementLinkedToIncomeException()
    {
        var bucket = _repository.ReserveBuckets.First(b => b.Name == "Investimento");
        var linked = ReserveMovement.Create(bucket, 100m, new DateOnly(2026, 7, 1), "August salary", LinkedIncome());
        _repository.ReserveMovements.Add(linked);

        var act = async () => await _sut.UpdateMovementAsync(linked.Id, new UpdateReserveMovementDTO
        {
            BucketId = BucketId("HouseTreats"),
            Amount = 150m,
            Date = new DateOnly(2026, 7, 5),
            Description = "Corrected"
        });

        await act.Should().ThrowAsync<ReserveMovementLinkedToIncomeException>().WithMessage("*linked to an income*");
        linked.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task DeleteMovementAsync_OnLinkedMovement_ThrowsReserveMovementLinkedToIncomeException()
    {
        var bucket = _repository.ReserveBuckets.First(b => b.Name == "Investimento");
        var linked = ReserveMovement.Create(bucket, 100m, new DateOnly(2026, 7, 1), "August salary", LinkedIncome());
        _repository.ReserveMovements.Add(linked);

        var act = async () => await _sut.DeleteMovementAsync(linked.Id);

        await act.Should().ThrowAsync<ReserveMovementLinkedToIncomeException>().WithMessage("*linked to an income*");
        _repository.ReserveMovements.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteMovementAsync_OnUnlinkedMovementSharingDateAndDescriptionWithALinkedOne_DeletesOnlyTheUnlinkedGroup()
    {
        var investimento = _repository.ReserveBuckets.First(b => b.Name == "Investimento");
        var houseTreats = _repository.ReserveBuckets.First(b => b.Name == "HouseTreats");
        var sameDate = new DateOnly(2026, 7, 1);
        var manual1 = ReserveMovement.Create(investimento, 50m, sameDate, "August salary");
        var manual2 = ReserveMovement.Create(houseTreats, 50m, sameDate, "August salary");
        var linked = ReserveMovement.Create(investimento, 100m, sameDate, "August salary", LinkedIncome());
        _repository.ReserveMovements.AddRange([manual1, manual2, linked]);

        await _sut.DeleteMovementAsync(manual1.Id);

        using (new AssertionScope())
        {
            _repository.ReserveMovements.Should().ContainSingle().Which.Should().BeSameAs(linked);
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_StillCreatesUnlinkedMovements()
    {
        await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        _repository.ReserveMovements.Should().OnlyContain(m => m.Income == null);
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
