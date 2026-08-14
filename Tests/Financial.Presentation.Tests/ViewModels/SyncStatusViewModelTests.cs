using Financial.Presentation.App.ViewModels;
using Financial.Shared.Infrastructure.Sync;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class SyncStatusViewModelTests
{
    [Fact]
    public void Constructor_PopulatesStatusImmediately_FromBothRepositories()
    {
        var cashFlowRepository = new SyncStatusCashFlowRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Pending, null, null),
        };
        var investmentRepository = new SyncStatusRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Saving, null, null),
        };

        var vm = new SyncStatusViewModel(cashFlowRepository, investmentRepository);

        vm.CashFlowStatus.Should().Be(new SyncStatus(SyncState.Pending, null, null));
        vm.InvestmentStatus.Should().Be(new SyncStatus(SyncState.Saving, null, null));
    }

    [Fact]
    public void Constructor_WithNullCashFlowRepository_Throws()
    {
        Action act = () => new SyncStatusViewModel(null!, new SyncStatusRepositoryStub());

        act.Should().Throw<ArgumentNullException>().WithParameterName("cashFlowRepository");
    }

    [Fact]
    public void Constructor_WithNullInvestmentRepository_Throws()
    {
        Action act = () => new SyncStatusViewModel(new SyncStatusCashFlowRepositoryStub(), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("investmentRepository");
    }

    [Fact]
    public void RefreshStatus_WhenRepositoryIsNotASyncStatusProvider_ReportsIdle()
    {
        var vm = new SyncStatusViewModel(new StubCashFlowRepository(), new StubRepository());

        vm.RefreshStatus();

        vm.CashFlowStatus.Should().Be(new SyncStatus(SyncState.Idle, null, null));
        vm.InvestmentStatus.Should().Be(new SyncStatus(SyncState.Idle, null, null));
    }

    [Fact]
    public void RefreshStatus_ReflectsUpdatedStatusOnEachCall()
    {
        var cashFlowRepository = new SyncStatusCashFlowRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Idle, null, null),
        };
        var vm = new SyncStatusViewModel(cashFlowRepository, new SyncStatusRepositoryStub());

        cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null);
        vm.RefreshStatus();

        vm.CashFlowStatus.Should().Be(new SyncStatus(SyncState.Failed, "Drive unreachable", null));
    }

    [Fact]
    public void RefreshStatus_CashFlowAndInvestmentAreIndependent()
    {
        var cashFlowRepository = new SyncStatusCashFlowRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Failed, "CashFlow drive error.", null),
        };
        var investmentRepository = new SyncStatusRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Idle, null, null),
        };

        var vm = new SyncStatusViewModel(cashFlowRepository, investmentRepository);
        vm.RefreshStatus();

        vm.CashFlowStatus.State.Should().Be(SyncState.Failed);
        vm.InvestmentStatus.State.Should().Be(SyncState.Idle);
    }
}
