using Financial.CashFlow.Application.Interfaces;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using Financial.Shared.Abstractions.Sync;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class SyncStatusViewModelTests
{
    private readonly SyncStatusCashFlowRepositoryStub _cashFlowRepository;
    private readonly SyncStatusInvestmentRepositoryStub _investmentRepository;

    public SyncStatusViewModelTests()
    {
        _cashFlowRepository = new SyncStatusCashFlowRepositoryStub();
        _investmentRepository = new SyncStatusInvestmentRepositoryStub();
    }

    /// <summary>The view model reads both repositories in its own constructor, so each test configures
    /// the shared stubs first and builds the view model here rather than re-wiring it by hand.</summary>
    private SyncStatusViewModel CreateViewModel(
        ICashFlowRepository? cashFlowRepository = null, IInvestmentRepository? investmentRepository = null) =>
        new(cashFlowRepository ?? _cashFlowRepository, investmentRepository ?? _investmentRepository);

    [Fact]
    public void Constructor_PopulatesStatusImmediately_FromBothRepositories()
    {
        _cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Pending, null, null);
        _investmentRepository.StatusToReturn = new SyncStatus(SyncState.Saving, null, null);

        var vm = CreateViewModel();

        vm.CashFlowStatus.Should().Be(new SyncStatus(SyncState.Pending, null, null));
        vm.InvestmentStatus.Should().Be(new SyncStatus(SyncState.Saving, null, null));
    }

    [Fact]
    public void Constructor_WithNullCashFlowRepository_Throws()
    {
        Action act = () => new SyncStatusViewModel(null!, _investmentRepository);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cashFlowRepository");
    }

    [Fact]
    public void Constructor_WithNullInvestmentRepository_Throws()
    {
        Action act = () => new SyncStatusViewModel(_cashFlowRepository, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("investmentRepository");
    }

    [Fact]
    public void RefreshStatus_WhenRepositoryIsNotASyncStatusProvider_ReportsIdle()
    {
        var vm = CreateViewModel(new StubCashFlowRepository(), new StubInvestmentRepository());

        vm.RefreshStatus();

        vm.CashFlowStatus.Should().Be(new SyncStatus(SyncState.Idle, null, null));
        vm.InvestmentStatus.Should().Be(new SyncStatus(SyncState.Idle, null, null));
    }

    [Fact]
    public void RefreshStatus_ReflectsUpdatedStatusOnEachCall()
    {
        _cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Idle, null, null);
        var vm = CreateViewModel();

        _cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null);
        vm.RefreshStatus();

        vm.CashFlowStatus.Should().Be(new SyncStatus(SyncState.Failed, "Drive unreachable", null));
    }

    [Fact]
    public void RefreshStatus_CashFlowAndInvestmentAreIndependent()
    {
        _cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Failed, "CashFlow drive error.", null);
        _investmentRepository.StatusToReturn = new SyncStatus(SyncState.Idle, null, null);

        var vm = CreateViewModel();
        vm.RefreshStatus();

        vm.CashFlowStatus.State.Should().Be(SyncState.Failed);
        vm.InvestmentStatus.State.Should().Be(SyncState.Idle);
    }

    [Fact]
    public void IsIndicatorVisible_BothContextsHealthy_IsFalse()
    {
        var vm = CreateViewModel();

        vm.IsIndicatorVisible.Should().BeFalse();
        vm.IndicatorMessages.Should().BeEmpty();
    }

    [Fact]
    public void IsIndicatorVisible_CashFlowFailed_IsTrue()
    {
        _cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null);

        var vm = CreateViewModel();

        vm.IsIndicatorVisible.Should().BeTrue();
        vm.IndicatorMessages.Should().ContainSingle(message => message.StartsWith("CashFlow changes could not be saved"));
    }

    [Fact]
    public void IsIndicatorVisible_InvestmentFailed_IsTrue()
    {
        _investmentRepository.StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null);

        var vm = CreateViewModel();

        vm.IsIndicatorVisible.Should().BeTrue();
        vm.IndicatorMessages.Should().ContainSingle(message => message.StartsWith("Investment changes could not be saved"));
    }

    [Fact]
    public void IndicatorMessages_BothContextsFailed_NamesBoth()
    {
        _cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Failed, "CashFlow drive error.", null);
        _investmentRepository.StatusToReturn = new SyncStatus(SyncState.Failed, "Investment drive error.", null);

        var vm = CreateViewModel();

        vm.IndicatorMessages.Should().HaveCount(2);
        vm.IndicatorMessages.Should().Contain(message => message.StartsWith("CashFlow changes could not be saved"));
        vm.IndicatorMessages.Should().Contain(message => message.StartsWith("Investment changes could not be saved"));
    }

    [Fact]
    public void IndicatorMessages_IncludesLastErrorAndFormattedSaveTime()
    {
        var lastSuccessfulSaveUtc = new DateTime(2026, 8, 13, 9, 12, 0, DateTimeKind.Utc);
        _cashFlowRepository.StatusToReturn = new SyncStatus(
            SyncState.Failed,
            "Drive request failed with a transient status (503 ServiceUnavailable).",
            lastSuccessfulSaveUtc);

        var vm = CreateViewModel();

        vm.IndicatorMessages.Should().ContainSingle().Which.Should().Be(
            "CashFlow changes could not be saved to Google Drive (last error: Drive request failed with a " +
            "transient status (503 ServiceUnavailable).). Last successful save: 13/08/2026 09:12.");
    }

    [Fact]
    public void IndicatorMessages_NoPriorSuccessfulSave_ShowsNever()
    {
        _cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null);

        var vm = CreateViewModel();

        vm.IndicatorMessages.Should().ContainSingle().Which.Should().Contain("Last successful save: Never.");
    }

    [Fact]
    public void IsIndicatorVisible_AndIndicatorMessages_UpdateAfterRefreshStatus()
    {
        _cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null);
        var vm = CreateViewModel();
        vm.IsIndicatorVisible.Should().BeTrue();

        _cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Idle, null, DateTime.UtcNow);
        vm.RefreshStatus();

        vm.IsIndicatorVisible.Should().BeFalse();
        vm.IndicatorMessages.Should().BeEmpty();
    }
}
