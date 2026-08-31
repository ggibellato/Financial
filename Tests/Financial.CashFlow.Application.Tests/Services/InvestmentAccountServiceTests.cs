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

public class InvestmentAccountServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<InvestmentAccountService> Logger = NullLogger<InvestmentAccountService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly InvestmentAccountService _sut;

    public InvestmentAccountServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private InvestmentAccountService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new InvestmentAccountService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new InvestmentAccountService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetInvestmentAccounts_MapsEveryRepositoryAccountToADto()
    {
        var chaseSave = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var platinumVisa = InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);
        _repository.InvestmentAccounts.Add(chaseSave);
        _repository.InvestmentAccounts.Add(platinumVisa);

        var result = _sut.GetInvestmentAccounts();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            var chaseSaveDto = result.Should().ContainSingle(a => a.Name == "ChaseSave").Which;
            chaseSaveDto.Id.Should().Be(chaseSave.Id);
            chaseSaveDto.IsActive.Should().BeTrue();
            chaseSaveDto.IsLiability.Should().BeFalse();
            var platinumVisaDto = result.Should().ContainSingle(a => a.Name == "PlatinumVisa8003").Which;
            platinumVisaDto.IsLiability.Should().BeTrue();
        }
    }

    [Fact]
    public void GetInvestmentAccounts_DoesNotFilterByIsActive()
    {
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("RetiredAccount", isActive: false, isLiability: false));

        var result = _sut.GetInvestmentAccounts();

        result.Should().ContainSingle(a => a.Name == "RetiredAccount" && !a.IsActive);
    }

    [Fact]
    public void GetInvestmentAccounts_WithNoAccounts_ReturnsEmptyList()
    {
        var result = _sut.GetInvestmentAccounts();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new InvestmentAccountService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_WithValidRequest_AddsAndSaves()
    {
        var request = new InvestmentAccountCreateDTO { Name = "ChaseSave", IsActive = true, IsLiability = false, Aliases = ["Chase Save"] };

        var result = await _sut.CreateInvestmentAccountAsync(request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("ChaseSave");
            result.Aliases.Should().ContainSingle("Chase Save");
            result.LatestBalance.Should().Be(0m);
            _repository.InvestmentAccounts.Should().ContainSingle(a => a.Name == "ChaseSave");
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateInvestmentAccountAsync_WithoutAName_ThrowsAndWritesNothing(string? name)
    {
        var request = new InvestmentAccountCreateDTO { Name = name!, IsActive = true, IsLiability = false, Aliases = [] };

        var act = async () => await _sut.CreateInvestmentAccountAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<ArgumentException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateInvestmentAccountAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));
        var request = new InvestmentAccountCreateDTO { Name = "ChaseSave", IsActive = true, IsLiability = false, Aliases = [] };

        var act = async () => await _sut.CreateInvestmentAccountAsync(request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_WithValidRequest_UpdatesAndSaves()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        var request = new InvestmentAccountUpdateDTO { Name = "ChaseSaveRenamed", IsActive = false, IsLiability = true, Aliases = ["New alias"] };

        var result = await _sut.UpdateInvestmentAccountAsync(account.Id, request);

        using (new AssertionScope())
        {
            result.Name.Should().Be("ChaseSaveRenamed");
            result.IsActive.Should().BeFalse();
            result.IsLiability.Should().BeTrue();
            result.Aliases.Should().ContainSingle("New alias");
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var request = new InvestmentAccountUpdateDTO { Name = "X", IsActive = true, IsLiability = false, Aliases = [] };

        var act = async () => await _sut.UpdateInvestmentAccountAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateInvestmentAccountAsync_WithDuplicateName_ThrowsAndWritesNothing()
    {
        var chaseSave = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var baAmex = InvestmentAccount.Create("BaAmex", isActive: true, isLiability: true);
        _repository.InvestmentAccounts.Add(chaseSave);
        _repository.InvestmentAccounts.Add(baAmex);
        var request = new InvestmentAccountUpdateDTO { Name = "BaAmex", IsActive = true, IsLiability = false, Aliases = [] };

        var act = async () => await _sut.UpdateInvestmentAccountAsync(chaseSave.Id, request);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<DuplicateNameException>();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_WithNoSnapshot_RemovesAndSaves()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);

        await _sut.DeleteInvestmentAccountAsync(account.Id);

        using (new AssertionScope())
        {
            _repository.InvestmentAccounts.Should().BeEmpty();
            _repository.SaveChangesCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_WithZeroLatestSnapshot_RemovesAndSaves()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 6, 0m));

        await _sut.DeleteInvestmentAccountAsync(account.Id);

        _repository.InvestmentAccounts.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteInvestmentAccountAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_WithNonZeroLatestSnapshot_ThrowsAndWritesNothing()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 6, 500m));

        var act = async () => await _sut.DeleteInvestmentAccountAsync(account.Id);

        using (new AssertionScope())
        {
            await act.Should().ThrowAsync<EntityInUseException>();
            _repository.InvestmentAccounts.Should().ContainSingle();
            _repository.SaveChangesCallCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task DeleteInvestmentAccountAsync_UsesTheHighestYearMonthSnapshotNotTheLatestAdded()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 7, 500m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 6, 0m));

        var act = async () => await _sut.DeleteInvestmentAccountAsync(account.Id);

        await act.Should().ThrowAsync<EntityInUseException>();
    }

    [Fact]
    public void GetInvestmentAccounts_LatestBalance_ReflectsTheMostRecentSnapshotByYearThenMonth()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        _repository.InvestmentAccounts.Add(account);
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2025, 12, 100m));
        _repository.InvestmentSnapshots.Add(InvestmentSnapshot.Create(account, 2026, 1, 250m));

        var result = _sut.GetInvestmentAccounts();

        result.Should().ContainSingle(a => a.Id == account.Id).Which.LatestBalance.Should().Be(250m);
    }
}
