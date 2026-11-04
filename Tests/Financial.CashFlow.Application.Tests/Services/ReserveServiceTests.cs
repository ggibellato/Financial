using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class ReserveServiceTests
{
    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly ReserveService _sut;

    public ReserveServiceTests()
    {
        _repository = new StubCashFlowRepository(seedDefaultPots: true);
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private ReserveService CreateService(StubCashFlowRepository? repository = null, ILogger<ReserveService>? logger = null)
    {
        var target = repository ?? _repository;
        var potsService = new PotsService(target, TestClock.At(), _tracer, NullLogger<PotsService>.Instance);
        return new ReserveService(target, potsService, _tracer, logger ?? NullLogger<ReserveService>.Instance);
    }

    /// <summary>The seeded bucket's id. The stub repository generates a fresh Guid per test run,
    /// so a test names the bucket it means and lets this resolve the id it must send.</summary>
    private Guid BucketId(string name) => _repository.ReservaPots.First(b => b.Name == name).Id;

    [Fact]
    public async Task PostIncomeSplitAsync_WithValidRequest_PostsOneMovementPerActiveBucketAndReturnsAmounts()
    {
        var result = await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        using (new AssertionScope())
        {
            _repository.PotTransactions.Should().HaveCount(4);
            _repository.PotTransactions.Should().OnlyContain(m => m.Description == "Ramsay");
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
    public async Task PostIncomeSplitAsync_WithThirdsSplit_PersistsConservedMovements()
    {
        var repository = new StubCashFlowRepository();
        repository.AddPot(Pot.Create("Investimento", 33.33m));
        repository.AddPot(Pot.Create("HouseTreats", 33.33m));
        repository.AddPot(Pot.Create("Ariana", 33.34m));
        var sut = CreateService(repository);

        var result = await sut.PostIncomeSplitAsync(new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 100.01m,
            Description = "Ramsay"
        });

        using (new AssertionScope())
        {
            result.Buckets.Select(b => b.Amount).Should().Equal(33.33m, 33.33m, 33.35m);
            result.Total.Should().Be(100.01m);
            repository.PotTransactions.Select(m => m.Amount).Should().Equal(33.33m, 33.33m, 33.35m);
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithInactiveBucket_ExcludesItFromMovementsAndResult()
    {
        _repository.ReservaPots.Single(b => b.Name == "Gleison").Update("Gleison", 16.67m, isActive: false);
        _repository.ReservaPots.Single(b => b.Name == "Ariana").Update("Ariana", 33.34m, isActive: true);

        var result = await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        using (new AssertionScope())
        {
            _repository.PotTransactions.Should().HaveCount(3);
            result.Buckets.Should().HaveCount(3);
            result.Buckets.Should().NotContain(b => b.BucketName == "Gleison");
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithNoActiveBuckets_ThrowsArgumentExceptionBeforeTouchingRepository()
    {
        var repository = new StubCashFlowRepository();
        repository.AddPot(Pot.Create("Investimento", 33.33m, isActive: false));
        var service = CreateService(repository);

        var act = async () => await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*has no active pots.");
        repository.PotTransactions.Should().BeEmpty();
        repository.SaveChangesCallCount.Should().Be(0);
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostIncomeSplit");
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithoutAPotsAccount_ThrowsArgumentException()
    {
        var repository = new StubCashFlowRepository();
        var service = CreateService(repository);

        var act = async () => await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("No reserve bucket is currently active.");
        repository.PotTransactions.Should().BeEmpty();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostIncomeSplit");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostIncomeSplitAsync_WithNonPositiveAmount_ThrowsBeforeTouchingRepository(decimal amount)
    {
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = amount,
            Description = "Ramsay"
        };

        var act = async () => await _sut.PostIncomeSplitAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Amount must be greater than zero.");
        _repository.PotTransactions.Should().BeEmpty();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostIncomeSplit");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostIncomeSplitAsync_WithMissingDescription_ThrowsBeforeTouchingRepository(string? description)
    {
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = description!
        };

        var act = async () => await _sut.PostIncomeSplitAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Description is required.");
        _repository.PotTransactions.Should().BeEmpty();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostIncomeSplit");
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithAFutureDate_ThrowsAndSavesNothing()
    {
        var request = new IncomeSplitRequestDTO
        {
            Date = TestClock.Today.AddDays(1),
            Amount = 1963m,
            Description = "Ramsay"
        };

        var act = async () => await _sut.PostIncomeSplitAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        _repository.PotTransactions.Should().BeEmpty();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostIncomeSplit");
    }

    [Fact]
    public async Task PostIncomeSplitAsync_ActivePotsNotTotalling100_ThrowsAndSavesNothing()
    {
        _repository.ReservaPots.Single(b => b.Name == "Gleison").Update("Gleison", 16.67m, isActive: false);

        var act = async () => await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Active pots must total 100% (now 83.33%).");
        _repository.PotTransactions.Should().BeEmpty();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostIncomeSplit");
    }

    [Fact]
    public async Task PostIncomeSplitAsync_LinksEveryMovementWithOneOperationIdAndWritesNoExpense()
    {
        await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        using (new AssertionScope())
        {
            _repository.PotTransactions.Select(m => m.OperationId).Distinct().Should().ContainSingle().Which.Should().NotBeNull();
            _repository.Expenses.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WhenSaveFails_RecordsFailedSpanAndRethrows()
    {
        var repository = new StubCashFlowRepository(seedDefaultPots: true) { ThrowOnNextSave = true };
        var service = CreateService(repository);

        var act = async () => await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.ReserveService.PostIncomeSplit");
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
        _repository.PotTransactions.Should().HaveCount(2);
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
        _repository.PotTransactions.Should().HaveCount(1);
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
        _repository.PotTransactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithZeroAmount_ThrowsArgumentException()
    {
        var act = async () => await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 0m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Nothing"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task PostWithdrawalAsync_ZeroAmountFromAnOverdrawnPot_ThrowsAmountErrorNotOverdraft()
    {
        _repository.Seed("Investimento", -10m);

        var act = async () => await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 0m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Nothing"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Amount must be greater than zero.");
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostWithdrawal");
    }

    [Fact]
    public async Task PostWithdrawalAsync_PotOfASecondPotsAccount_ThrowsNotRecognizedAndSavesNothing()
    {
        var holidays = TestAccounts.Pots("Holiday pots");
        var beach = Pot.Create("Beach", 0m);
        holidays.AddPot(beach);
        _repository.Accounts.Add(holidays);

        var act = async () => await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = beach.Id,
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Towels"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not recognized*");
        _repository.PotTransactions.Should().BeEmpty();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostWithdrawal");
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithAFutureDate_ThrowsAndSavesNothing()
    {
        _repository.Seed("Investimento", 100m);

        var act = async () => await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 30m,
            Date = TestClock.Today.AddDays(1),
            Description = "Groceries top-up"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Date must not be in the future.");
        _repository.PotTransactions.Should().ContainSingle();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostWithdrawal");
    }

    [Fact]
    public async Task PostWithdrawalAsync_ReturnsTheMovementItSaved()
    {
        _repository.Seed("Investimento", 100m);

        var result = await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 30m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Groceries top-up"
        });

        var saved = _repository.PotTransactions.Single(m => m.Id == result.Id);
        using (new AssertionScope())
        {
            result.BucketId.Should().Be(BucketId("Investimento"));
            result.BucketName.Should().Be("Investimento");
            result.Amount.Should().Be(-30m);
            result.Date.Should().Be(new DateOnly(2026, 7, 1));
            result.Description.Should().Be("Groceries top-up");
            result.IncomeId.Should().BeNull();
            saved.OperationId.Should().NotBeNull();
        }
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
        var retiredGleison = _repository.ReservaPots.Single(b => b.Name == "Gleison");
        retiredGleison.Update("Gleison", 16.67m, isActive: false);
        _repository.PotTransactions.Add(PotTransaction.Create(retiredGleison, 75m, new DateOnly(2026, 7, 1), "Before retirement"));

        var balances = _sut.GetBucketBalances();

        balances.Should().HaveCount(4);
        balances.Should().ContainSingle(b => b.BucketName == "Gleison" && b.Balance == 75m);
    }

    [Fact]
    public void GetBucketBalances_IsNotHardcodedToFourBuckets()
    {
        _repository.AddPot(Pot.Create("Emergency", 0m, isActive: false));

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
    public async Task GetMovementHistory_LineOfAManualSplit_CountsTheLinesItsDeleteRemoves()
    {
        var date = new DateOnly(2026, 7, 1);
        var line = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Ramsay");
        var otherLine = PotTransaction.Create(_repository.ReservaPots.Last(), 10m, date, "Ramsay");
        var withdrawal = PotTransaction.Create(_repository.ReservaPots.First(), -5m, date, "Ramsay");
        var otherDate = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date.AddDays(1), "Ramsay");
        var otherDescription = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Bonus");
        _repository.PotTransactions.AddRange([line, otherLine, withdrawal, otherDate, otherDescription]);

        var history = _sut.GetMovementHistory();

        history.Single(m => m.Id == line.Id).SplitLineCount.Should().Be(2);
        await _sut.DeleteMovementAsync(line.Id);
        _repository.PotTransactions.Should().Equal(withdrawal, otherDate, otherDescription);
    }

    [Fact]
    public void GetMovementHistory_LineOutsideAManualSplit_CountsOne()
    {
        var date = new DateOnly(2026, 7, 1);
        var line = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Ramsay");
        var otherLine = PotTransaction.Create(_repository.ReservaPots.Last(), 10m, date, "Ramsay");
        var withdrawal = PotTransaction.Create(_repository.ReservaPots.First(), -5m, date, "Ramsay");
        var zeroLine = PotTransaction.Create(_repository.ReservaPots.First(), 0m, date, "Ramsay");
        var otherDate = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date.AddDays(1), "Ramsay");
        var otherDescription = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Bonus");
        _repository.PotTransactions.AddRange([line, otherLine, withdrawal, zeroLine, otherDate, otherDescription]);

        var history = _sut.GetMovementHistory();

        using (new AssertionScope())
        {
            history.Single(m => m.Id == withdrawal.Id).SplitLineCount.Should().Be(1);
            history.Single(m => m.Id == zeroLine.Id).SplitLineCount.Should().Be(1);
            history.Single(m => m.Id == otherDate.Id).SplitLineCount.Should().Be(1);
            history.Single(m => m.Id == otherDescription.Id).SplitLineCount.Should().Be(1);
        }
    }

    [Fact]
    public void GetMovementHistory_LinkedLinesSharingDateAndDescription_CountOneAndStayOutOfTheManualSplit()
    {
        var date = new DateOnly(2026, 7, 1);
        var manual = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Ramsay");
        var otherManual = PotTransaction.Create(_repository.ReservaPots.Last(), 10m, date, "Ramsay");
        var incomeLine = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Ramsay", LinkedIncome());
        var operationLine = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Ramsay", operationId: Guid.NewGuid());
        _repository.PotTransactions.AddRange([manual, otherManual, incomeLine, operationLine]);

        var history = _sut.GetMovementHistory();

        using (new AssertionScope())
        {
            history.Single(m => m.Id == manual.Id).SplitLineCount.Should().Be(2);
            history.Single(m => m.Id == incomeLine.Id).SplitLineCount.Should().Be(1);
            history.Single(m => m.Id == operationLine.Id).SplitLineCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateMovementAsync_LineMovedOutOfItsManualSplit_CountsOne()
    {
        var date = new DateOnly(2026, 7, 1);
        var line = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Ramsay");
        var otherLine = PotTransaction.Create(_repository.ReservaPots.Last(), 10m, date, "Ramsay");
        _repository.PotTransactions.AddRange([line, otherLine]);

        var result = await _sut.UpdateMovementAsync(line.Id, new ReserveMovementUpdateDTO
        {
            BucketId = line.Pot.Id,
            Amount = 10m,
            Date = date,
            Description = "Corrected"
        });

        result.SplitLineCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateMovementAsync_ExistingId_UpdatesFieldsAndSaves()
    {
        _repository.Seed("Investimento", 100m, new DateOnly(2026, 7, 1));
        var movement = _repository.PotTransactions[0];

        var result = await _sut.UpdateMovementAsync(movement.Id, new ReserveMovementUpdateDTO
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
        var act = async () => await _sut.UpdateMovementAsync(Guid.NewGuid(), new ReserveMovementUpdateDTO
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

        var act = async () => await _sut.UpdateMovementAsync(_repository.PotTransactions[0].Id, new ReserveMovementUpdateDTO
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
        var toDelete = _repository.PotTransactions[0];
        _repository.Seed("Ariana", -20m, new DateOnly(2026, 7, 2));

        await _sut.DeleteMovementAsync(toDelete.Id);

        _repository.PotTransactions.Should().ContainSingle();
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteMovementAsync_MovementFromALegacySplit_DeletesAllFourSiblingMovements()
    {
        foreach (var pot in _repository.ReservaPots)
        {
            _repository.PotTransactions.Add(PotTransaction.Create(pot, 10m, new DateOnly(2026, 7, 1), "Ramsay"));
        }

        await _sut.DeleteMovementAsync(_repository.PotTransactions[0].Id);

        _repository.PotTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteMovementAsync_LineOfAManualSplit_LeavesAWithdrawalWithTheSameDateAndDescription()
    {
        var date = new DateOnly(2026, 7, 1);
        var line = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Ramsay");
        var otherLine = PotTransaction.Create(_repository.ReservaPots.Last(), 10m, date, "Ramsay");
        var withdrawal = PotTransaction.Create(_repository.ReservaPots.First(), -5m, date, "Ramsay");
        _repository.PotTransactions.AddRange([line, otherLine, withdrawal]);

        await _sut.DeleteMovementAsync(line.Id);

        _repository.PotTransactions.Should().ContainSingle().Which.Should().BeSameAs(withdrawal);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    public async Task DeleteMovementAsync_NotPositiveMovement_DeletesItAlone(decimal amount)
    {
        var date = new DateOnly(2026, 7, 1);
        var line = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Ramsay");
        var notPositive = PotTransaction.Create(_repository.ReservaPots.First(), amount, date, "Ramsay");
        var otherWithdrawal = PotTransaction.Create(_repository.ReservaPots.Last(), -5m, date, "Ramsay");
        _repository.PotTransactions.AddRange([line, notPositive, otherWithdrawal]);

        await _sut.DeleteMovementAsync(notPositive.Id);

        _repository.PotTransactions.Should().Equal(line, otherWithdrawal);
    }

    [Fact]
    public async Task DeleteMovementAsync_LineOfAManualSplit_LeavesLinesWithAnotherDateOrDescription()
    {
        var date = new DateOnly(2026, 7, 1);
        var line = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Ramsay");
        var otherDate = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date.AddDays(1), "Ramsay");
        var otherDescription = PotTransaction.Create(_repository.ReservaPots.First(), 10m, date, "Bonus");
        _repository.PotTransactions.AddRange([line, otherDate, otherDescription]);

        await _sut.DeleteMovementAsync(line.Id);

        _repository.PotTransactions.Should().BeEquivalentTo([otherDate, otherDescription]);
    }

    [Fact]
    public async Task DeleteMovementAsync_LineOfASplitPostedThroughTheReservaRoute_ThrowsLinkedOperationConflictAndKeepsAll()
    {
        await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        var act = async () => await _sut.DeleteMovementAsync(_repository.PotTransactions[0].Id);

        await act.Should().ThrowAsync<LinkedOperationConflictException>();
        _repository.PotTransactions.Should().HaveCount(4);
        _tracer.ShouldHaveFailedSpan<LinkedOperationConflictException>("CashFlow.ReserveService.DeleteMovement");
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

    private static Income LinkedIncome()
    {
        var reserva = TestAccounts.Pots("Reserva");
        return Income.Create(
            new DateOnly(2026, 7, 1), IncomeSource.Create("Ariana", IncomeGroup.Salary, potsAccount: reserva), null, 2450m, null, "August salary", potsAccount: reserva);
    }

    [Fact]
    public async Task UpdateMovementAsync_OnLinkedMovement_ThrowsReserveMovementLinkedToIncomeException()
    {
        var pot = _repository.ReservaPots.First(b => b.Name == "Investimento");
        var linked = PotTransaction.Create(pot, 100m, new DateOnly(2026, 7, 1), "August salary", LinkedIncome());
        _repository.PotTransactions.Add(linked);

        var act = async () => await _sut.UpdateMovementAsync(linked.Id, new ReserveMovementUpdateDTO
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
        var pot = _repository.ReservaPots.First(b => b.Name == "Investimento");
        var linked = PotTransaction.Create(pot, 100m, new DateOnly(2026, 7, 1), "August salary", LinkedIncome());
        _repository.PotTransactions.Add(linked);

        var act = async () => await _sut.DeleteMovementAsync(linked.Id);

        await act.Should().ThrowAsync<ReserveMovementLinkedToIncomeException>().WithMessage("*linked to an income*");
        _repository.PotTransactions.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteMovementAsync_OnUnlinkedMovementSharingDateAndDescriptionWithALinkedOne_DeletesOnlyTheUnlinkedGroup()
    {
        var investimento = _repository.ReservaPots.First(b => b.Name == "Investimento");
        var houseTreats = _repository.ReservaPots.First(b => b.Name == "HouseTreats");
        var sameDate = new DateOnly(2026, 7, 1);
        var manual1 = PotTransaction.Create(investimento, 50m, sameDate, "August salary");
        var manual2 = PotTransaction.Create(houseTreats, 50m, sameDate, "August salary");
        var linked = PotTransaction.Create(investimento, 100m, sameDate, "August salary", LinkedIncome());
        _repository.PotTransactions.AddRange([manual1, manual2, linked]);

        await _sut.DeleteMovementAsync(manual1.Id);

        using (new AssertionScope())
        {
            _repository.PotTransactions.Should().ContainSingle().Which.Should().BeSameAs(linked);
        }
    }

    [Fact]
    public async Task PostIncomeSplitAsync_StillCreatesUnlinkedMovements()
    {
        await _sut.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        _repository.PotTransactions.Should().OnlyContain(m => m.Income == null);
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithBlankDescription_ThrowsArgumentException()
    {
        var act = async () => await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 30m,
            Date = new DateOnly(2026, 7, 1),
            Description = "   "
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Description is required*");
    }

    [Fact]
    public async Task PostWithdrawalAsync_WhenSaveFails_RecordsFailedSpanAndRethrows()
    {
        _repository.Seed("Investimento", 100m);
        _repository.ThrowOnNextSave = true;

        var act = async () => await _sut.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 30m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Groceries top-up",
            Confirmed = false
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.ReserveService.PostWithdrawal");
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithoutBank_CreatesNoExpenses()
    {
        var repository = CreateBankRepository();

        await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository, withBank: false, withCategory: false));

        using (new AssertionScope())
        {
            repository.PotTransactions.Should().HaveCount(2);
            repository.Expenses.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithBankAndCategory_CreatesMovementAndTwoExpenses()
    {
        var repository = CreateBankRepository();
        var request = BankWithdrawal(repository);

        await CreateService(repository).PostWithdrawalAsync(request);

        using (new AssertionScope())
        {
            repository.PotTransactions.Should().ContainSingle(m => m.Amount == -request.Amount);
            repository.Expenses.Should().HaveCount(2);
            repository.Expenses.Should().OnlyContain(e =>
                e.PaymentSourceAccount!.Id == request.PaymentSourceBankId
                && e.CreditCard == null
                && e.Date == request.Date
                && e.Description == request.Description);
            repository.Expenses.Should().ContainSingle(e => e.Category == repository.EnsureReserva().Category && e.Value == -request.Amount);
            repository.Expenses.Should().ContainSingle(e => e.Category.Id == request.ExpenseCategoryId && e.Value == request.Amount);
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithBankAndCategory_LinksTheMovementAndBothExpensesWithOneOperationId()
    {
        var repository = CreateBankRepository();
        var request = BankWithdrawal(repository);

        await CreateService(repository).PostWithdrawalAsync(request);

        var movement = repository.PotTransactions.Single(m => m.Amount == -request.Amount);
        using (new AssertionScope())
        {
            movement.OperationId.Should().NotBeNull();
            repository.Expenses.Should().HaveCount(2).And.OnlyContain(e => e.OperationId == movement.OperationId);
        }
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithoutBank_GivesTheMovementAnOperationId()
    {
        var repository = CreateBankRepository();
        var request = BankWithdrawal(repository, withBank: false, withCategory: false);

        await CreateService(repository).PostWithdrawalAsync(request);

        repository.PotTransactions.Single(m => m.Amount == -request.Amount).OperationId.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateMovementAsync_MovementOfAPotOperation_ThrowsLinkedOperationConflictAndKeepsIt()
    {
        var linked = AddOperationMovement();

        var act = async () => await _sut.UpdateMovementAsync(linked.Id, new ReserveMovementUpdateDTO
        {
            BucketId = BucketId("HouseTreats"),
            Amount = -50m,
            Date = new DateOnly(2026, 7, 5),
            Description = "Corrected"
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<LinkedOperationConflictException>()
                .WithMessage("This movement belongs to a pot deposit or withdrawal; edit it from the Pots account.");
            linked.Amount.Should().Be(-30m);
            linked.Description.Should().Be("Boots");
            _tracer.ShouldHaveFailedSpan<LinkedOperationConflictException>("CashFlow.ReserveService.UpdateMovement");
        }
    }

    [Fact]
    public async Task DeleteMovementAsync_MovementOfAPotOperation_ThrowsLinkedOperationConflictAndKeepsIt()
    {
        var linked = AddOperationMovement();

        var act = async () => await _sut.DeleteMovementAsync(linked.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<LinkedOperationConflictException>()
                .WithMessage("This movement belongs to a pot deposit or withdrawal; edit it from the Pots account.");
            _repository.PotTransactions.Should().ContainSingle().Which.Should().BeSameAs(linked);
            _tracer.ShouldHaveFailedSpan<LinkedOperationConflictException>("CashFlow.ReserveService.DeleteMovement");
        }
    }

    [Fact]
    public async Task DeleteMovementAsync_LegacyMovementSharingDateAndDescriptionWithAnOperationMovement_LeavesTheOperationMovement()
    {
        var linked = AddOperationMovement();
        var legacy = PotTransaction.Create(_repository.ReservaPots.First(b => b.Name == "HouseTreats"), 30m, linked.Date, linked.Description);
        _repository.PotTransactions.Add(legacy);

        await _sut.DeleteMovementAsync(legacy.Id);

        _repository.PotTransactions.Should().ContainSingle().Which.Should().BeSameAs(linked);
    }

    private PotTransaction AddOperationMovement()
    {
        var linked = PotTransaction.Create(
            _repository.ReservaPots.First(b => b.Name == "Investimento"), -30m, new DateOnly(2026, 7, 1), "Boots", operationId: Guid.NewGuid());
        _repository.PotTransactions.Add(linked);
        return linked;
    }

    [Fact]
    public async Task PostWithdrawalAsync_BankWithoutCategory_Throws()
    {
        var repository = CreateBankRepository();

        var act = async () => await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository, withCategory: false));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Category is required when a bank is selected*");
        AssertNothingSaved(repository);
    }

    [Fact]
    public async Task PostWithdrawalAsync_CategoryWithoutBank_Throws()
    {
        var repository = CreateBankRepository();

        var act = async () => await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository, withBank: false));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*bank is required*");
        AssertNothingSaved(repository);
    }

    [Fact]
    public async Task PostWithdrawalAsync_UnknownBank_Throws()
    {
        var repository = CreateBankRepository();

        var act = async () => await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository, bankId: Guid.NewGuid()));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
        AssertNothingSaved(repository);
    }

    [Fact]
    public async Task PostWithdrawalAsync_UnknownCategory_Throws()
    {
        var repository = CreateBankRepository();

        var act = async () => await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository, categoryId: Guid.NewGuid()));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Category*not recognized*");
        AssertNothingSaved(repository);
    }

    [Fact]
    public async Task PostWithdrawalAsync_InactiveCategory_Throws()
    {
        var repository = CreateBankRepository();
        var inactive = Category.Create("Old", isActive: false);
        repository.Categories.Add(inactive);

        var act = async () => await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository, categoryId: inactive.Id));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*inactive*");
        AssertNothingSaved(repository);
    }

    [Theory]
    [InlineData("Investimento")]
    [InlineData("Reserva")]
    public async Task PostWithdrawalAsync_InvestmentOrReservasCategory_Throws(string categoryName)
    {
        var repository = CreateBankRepository();
        var category = repository.Categories.First(c => c.Name == categoryName);

        var act = async () => await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository, categoryId: category.Id));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*cannot be used for a withdrawal expense*");
        AssertNothingSaved(repository);
    }

    [Fact]
    public async Task PostWithdrawalAsync_CategoryOfASecondPotsAccount_Throws()
    {
        var repository = CreateBankRepository();
        var holidays = Category.Create("Holidays");
        repository.Categories.Add(holidays);
        repository.Accounts.Add(TestAccounts.Pots("Holiday pots", category: holidays));

        var act = async () => await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository, categoryId: holidays.Id));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Category 'Holidays' cannot be used for a withdrawal expense.");
        AssertNothingSaved(repository);
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostWithdrawal");
    }

    [Fact]
    [Trait("AC", "P60-F09-pots-template-and-reserva-migration-04")]
    public async Task PostWithdrawalAsync_AfterRenamingReservasCategory_BooksTheBankExpenseInIt()
    {
        var repository = CreateBankRepository();
        var reservaCategory = repository.EnsureReserva().Category!;
        reservaCategory.Update("Savings", active: true, isInvestment: false, isTithe: false);
        var request = BankWithdrawal(repository);

        await CreateService(repository).PostWithdrawalAsync(request);

        repository.Expenses.Should().ContainSingle(e => e.Category == reservaCategory && e.Value == -request.Amount);
    }

    [Fact]
    public async Task PostWithdrawalAsync_ReservaCategoryInactive_StillSucceeds()
    {
        var repository = CreateBankRepository();
        var reserva = repository.EnsureReserva().Category!;
        reserva.Update(reserva.Name, active: false, isInvestment: false, isTithe: false);

        await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository));

        repository.Expenses.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostWithdrawalAsync_ReservaWithoutACategory_Throws()
    {
        var repository = CreateBankRepository(withReservaCategory: false);

        var act = async () => await CreateService(repository).PostWithdrawalAsync(BankWithdrawal(repository));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Set a category on Reserva first.");
        AssertNothingSaved(repository);
        _tracer.ShouldHaveFailedSpan<ArgumentException>("CashFlow.ReserveService.PostWithdrawal");
    }

    [Fact]
    public async Task PostWithdrawalAsync_BankPathOverdraft_RequiresConfirmation()
    {
        var repository = CreateBankRepository();
        var request = BankWithdrawal(repository, amount: 500m);
        var service = CreateService(repository);

        var act = async () => await service.PostWithdrawalAsync(request);

        await act.Should().ThrowAsync<OverdraftConfirmationRequiredException>();
        AssertNothingSaved(repository, movements: 1);

        await service.PostWithdrawalAsync(BankWithdrawal(repository, amount: 500m, confirmed: true));
        repository.Expenses.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostWithdrawalAsync_BankPathDescriptionTooLong_Throws()
    {
        var repository = CreateBankRepository();
        var request = BankWithdrawal(repository, description: new string('x', 201));

        var act = async () => await CreateService(repository).PostWithdrawalAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*must not exceed*");
        AssertNothingSaved(repository);
    }

    [Fact]
    public async Task PostWithdrawalAsync_BankPathFailure_LogsNoDescriptionOrExceptionMessage()
    {
        var repository = CreateBankRepository();
        var logger = new RecordingLogger<ReserveService>();
        var service = CreateService(repository, logger);
        var request = BankWithdrawal(repository, withCategory: false, description: "Secret dentist");

        var act = async () => await service.PostWithdrawalAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        logger.Entries.Should().Contain(e => e.Message == "PostWithdrawal started");
        logger.Entries.Should().OnlyContain(e => !e.Message.Contains("Secret dentist") && !e.Message.Contains("Category is required"));
    }

    private static StubCashFlowRepository CreateBankRepository(bool withReservaCategory = true)
    {
        var repository = new StubCashFlowRepository(
            seedDefaultBanks: true, seedDefaultPots: true, seedDefaultCategories: true);
        if (withReservaCategory)
        {
            repository.EnsureReserva().SetCategory(repository.Categories.First(c => c.Name == "Reserva"));
        }

        repository.Seed("Investimento", 100m);
        return repository;
    }

    private static WithdrawalRequestDTO BankWithdrawal(
        StubCashFlowRepository repository,
        bool withBank = true,
        bool withCategory = true,
        Guid? bankId = null,
        Guid? categoryId = null,
        decimal amount = 30m,
        bool confirmed = false,
        string description = "Dentist") =>
        new()
        {
            BucketId = repository.ReservaPots.First(b => b.Name == "Investimento").Id,
            Amount = amount,
            Date = new DateOnly(2026, 7, 1),
            Description = description,
            Confirmed = confirmed,
            PaymentSourceBankId = withBank ? bankId ?? repository.Accounts.First(b => b.Name == "Chase").Id : null,
            ExpenseCategoryId = withCategory ? categoryId ?? repository.Categories.First(c => c.Name == "Saude").Id : null
        };

    private static void AssertNothingSaved(StubCashFlowRepository repository, int movements = 1)
    {
        using (new AssertionScope())
        {
            repository.Expenses.Should().BeEmpty();
            repository.PotTransactions.Should().HaveCount(movements, "only the seed movement may remain");
        }
    }

    [Fact]
    public void GetBucketBalances_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetBucketBalances();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.ReserveService.GetBucketBalances");
    }

    [Fact]
    public void GetMovementHistory_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnNextRead = new InvalidOperationException("simulated failure");

        Action act = () => _sut.GetMovementHistory();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("CashFlow.ReserveService.GetMovementHistory");
    }

    [Fact]
    public async Task UpdateMovementAsync_WithBlankDescription_ThrowsArgumentException()
    {
        _repository.Seed("Investimento", 100m, new DateOnly(2026, 7, 1));
        var movement = _repository.PotTransactions[0];

        var act = async () => await _sut.UpdateMovementAsync(movement.Id, new ReserveMovementUpdateDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 150m,
            Date = new DateOnly(2026, 7, 5),
            Description = " "
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Description is required*");
    }
}

internal static class ReserveServiceTestsStubExtensions
{
    public static void Seed(this StubCashFlowRepository repository, string bucketName, decimal amount, DateOnly? date = null)
    {
        var pot = repository.ReservaPots.First(b => b.Name == bucketName);
        repository.PotTransactions.Add(PotTransaction.Create(pot, amount, date ?? new DateOnly(2026, 1, 1), "Seed"));
    }

}
