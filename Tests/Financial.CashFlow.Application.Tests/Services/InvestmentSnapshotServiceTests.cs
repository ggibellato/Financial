using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class InvestmentSnapshotServiceTests
{
    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly int PastYear = CurrentYear - 5;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new InvestmentSnapshotService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_FirstCall_GeneratesExactlyElevenSnapshotsDefaultingToZero()
    {
        var repository = CreateRepository();
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(CurrentYear, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(11);
            result.Should().OnlyContain(s => s.Value == 0m);
            repository.InvestmentSnapshots.Should().HaveCount(11);
        }
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_MarksTheSixLiabilityAccountsCorrectly()
    {
        var repository = CreateRepository();
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(CurrentYear, 7);

        using (new AssertionScope())
        {
            result.Where(s => s.IsLiability).Should().HaveCount(6);
            result.Should().ContainSingle(s => s.AccountName == "PlatinumVisa8003" && s.IsLiability);
            result.Should().ContainSingle(s => s.AccountName == "ReservasPessoais" && s.IsLiability);
            result.Should().ContainSingle(s => s.AccountName == "ChaseSave" && !s.IsLiability);
        }
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_SecondCallSameMonth_DoesNotCreateDuplicates()
    {
        var repository = CreateRepository();
        var service = new InvestmentSnapshotService(repository);

        await service.GetSnapshotsForMonthAsync(CurrentYear, 7);
        var result = await service.GetSnapshotsForMonthAsync(CurrentYear, 7);

        using (new AssertionScope())
        {
            result.Should().HaveCount(11);
            repository.InvestmentSnapshots.Should().HaveCount(11);
            repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_CurrentYear_ExcludesDisabledAccounts()
    {
        var repository = CreateRepository();
        repository.InvestmentAccounts.Add(InvestmentAccount.Create("EverydaySaver", isActive: false, isLiability: false));
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(CurrentYear, 7);

        result.Should().HaveCount(11);
        result.Should().NotContain(s => s.AccountName == "EverydaySaver");
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_PastYearWithNoExistingData_ReturnsEmptyNotAllAccounts()
    {
        var repository = CreateRepository();
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(PastYear, 7);

        result.Should().BeEmpty();
        repository.InvestmentSnapshots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSnapshotsForMonthAsync_PastYearWithSomeAccountsPresent_ReturnsOnlyThose()
    {
        var repository = CreateRepository();
        var chaseSave = repository.InvestmentAccounts.First(a => a.Name == "ChaseSave");
        repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(chaseSave, PastYear, 7, 100m));
        var service = new InvestmentSnapshotService(repository);

        var result = await service.GetSnapshotsForMonthAsync(PastYear, 7);

        result.Should().ContainSingle().Which.AccountName.Should().Be("ChaseSave");
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_UpdatesOnlyTheTargetedSnapshot()
    {
        var repository = CreateRepository();
        var service = new InvestmentSnapshotService(repository);
        await service.GetSnapshotsForMonthAsync(CurrentYear, 7);
        await service.GetSnapshotsForMonthAsync(CurrentYear, 8);
        var julySnapshot = repository.InvestmentSnapshots.Single(s => s.Month == 7 && s.Account.Name == "ChaseSave");
        var augustSnapshot = repository.InvestmentSnapshots.Single(s => s.Month == 8 && s.Account.Name == "ChaseSave");
        var otherAccountSnapshot = repository.InvestmentSnapshots.Single(s => s.Month == 7 && s.Account.Name == "PlatinumVisa8003");

        var result = await service.UpdateSnapshotValueAsync(julySnapshot.Id, new UpdateInvestmentSnapshotValueDTO { Value = 500m });

        using (new AssertionScope())
        {
            result.Value.Should().Be(500m);
            augustSnapshot.Value.Should().Be(0m);
            otherAccountSnapshot.Value.Should().Be(0m);
        }
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_WithNegativeValue_ThrowsArgumentException()
    {
        var repository = CreateRepository();
        var service = new InvestmentSnapshotService(repository);
        await service.GetSnapshotsForMonthAsync(CurrentYear, 7);
        var snapshot = repository.InvestmentSnapshots.First();

        var act = async () => await service.UpdateSnapshotValueAsync(snapshot.Id, new UpdateInvestmentSnapshotValueDTO { Value = -1m });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateSnapshotValueAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new InvestmentSnapshotService(CreateRepository());

        var act = async () => await service.UpdateSnapshotValueAsync(Guid.NewGuid(), new UpdateInvestmentSnapshotValueDTO { Value = 10m });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static StubCashFlowRepository CreateRepository()
    {
        var repository = new StubCashFlowRepository();
        SeededInvestmentAccounts.SeedInto(repository);
        return repository;
    }
}
