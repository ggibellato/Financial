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

    private ReserveService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, NullLogger<ReserveService>.Instance);

    private Guid BucketId(string name) => _repository.ReservaPots.First(b => b.Name == name).Id;

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
    public async Task UpdateMovementAsync_MovementOfASecondPotsAccount_MovesItToAnotherPotOfThatAccount()
    {
        var (beach, trip) = AddSecondPotsAccount();
        var movement = PotTransaction.Create(beach, 40m, new DateOnly(2026, 7, 1), "Towels");
        _repository.PotTransactions.Add(movement);

        var result = await _sut.UpdateMovementAsync(movement.Id, new ReserveMovementUpdateDTO
        {
            BucketId = trip.Id,
            Amount = 40m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Towels"
        });

        using (new AssertionScope())
        {
            result.BucketName.Should().Be("Trip");
            movement.Pot.Should().BeSameAs(trip);
        }
    }

    [Fact]
    public async Task UpdateMovementAsync_PotOfAnotherPotsAccount_ThrowsNotRecognizedAndKeepsThePot()
    {
        var (beach, _) = AddSecondPotsAccount();
        var movement = PotTransaction.Create(beach, 40m, new DateOnly(2026, 7, 1), "Towels");
        _repository.PotTransactions.Add(movement);

        var act = async () => await _sut.UpdateMovementAsync(movement.Id, new ReserveMovementUpdateDTO
        {
            BucketId = BucketId("Investimento"),
            Amount = 40m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Towels"
        });

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not recognized*");
            movement.Pot.Should().BeSameAs(beach);
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    private (Pot Beach, Pot Trip) AddSecondPotsAccount()
    {
        var holidays = TestAccounts.Pots("Holiday pots");
        var beach = Pot.Create("Beach", 50m);
        var trip = Pot.Create("Trip", 50m);
        holidays.AddPot(beach);
        holidays.AddPot(trip);
        _repository.Accounts.Add(holidays);
        return (beach, trip);
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
    public async Task DeleteMovementAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = CreateService(new StubCashFlowRepository());

        var act = async () => await service.DeleteMovementAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

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
