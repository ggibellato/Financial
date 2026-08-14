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

    [Fact]
    public void IsIndicatorVisible_BothContextsHealthy_IsFalse()
    {
        var vm = new SyncStatusViewModel(new SyncStatusCashFlowRepositoryStub(), new SyncStatusRepositoryStub());

        vm.IsIndicatorVisible.Should().BeFalse();
        vm.IndicatorMessages.Should().BeEmpty();
    }

    [Fact]
    public void IsIndicatorVisible_CashFlowFailed_IsTrue()
    {
        var cashFlowRepository = new SyncStatusCashFlowRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null),
        };

        var vm = new SyncStatusViewModel(cashFlowRepository, new SyncStatusRepositoryStub());

        vm.IsIndicatorVisible.Should().BeTrue();
        vm.IndicatorMessages.Should().ContainSingle(message => message.StartsWith("CashFlow changes could not be saved"));
    }

    [Fact]
    public void IsIndicatorVisible_InvestmentFailed_IsTrue()
    {
        var investmentRepository = new SyncStatusRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null),
        };

        var vm = new SyncStatusViewModel(new SyncStatusCashFlowRepositoryStub(), investmentRepository);

        vm.IsIndicatorVisible.Should().BeTrue();
        vm.IndicatorMessages.Should().ContainSingle(message => message.StartsWith("Investment changes could not be saved"));
    }

    [Fact]
    public void IndicatorMessages_BothContextsFailed_NamesBoth()
    {
        var cashFlowRepository = new SyncStatusCashFlowRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Failed, "CashFlow drive error.", null),
        };
        var investmentRepository = new SyncStatusRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Failed, "Investment drive error.", null),
        };

        var vm = new SyncStatusViewModel(cashFlowRepository, investmentRepository);

        vm.IndicatorMessages.Should().HaveCount(2);
        vm.IndicatorMessages.Should().Contain(message => message.StartsWith("CashFlow changes could not be saved"));
        vm.IndicatorMessages.Should().Contain(message => message.StartsWith("Investment changes could not be saved"));
    }

    [Fact]
    public void IndicatorMessages_IncludesLastErrorAndFormattedSaveTime()
    {
        var lastSuccessfulSaveUtc = new DateTime(2026, 8, 13, 9, 12, 0, DateTimeKind.Utc);
        var cashFlowRepository = new SyncStatusCashFlowRepositoryStub
        {
            StatusToReturn = new SyncStatus(
                SyncState.Failed,
                "Drive request failed with a transient status (503 ServiceUnavailable).",
                lastSuccessfulSaveUtc),
        };

        var vm = new SyncStatusViewModel(cashFlowRepository, new SyncStatusRepositoryStub());

        vm.IndicatorMessages.Should().ContainSingle().Which.Should().Be(
            "CashFlow changes could not be saved to Google Drive (last error: Drive request failed with a " +
            "transient status (503 ServiceUnavailable).). Last successful save: 13/08/2026 09:12.");
    }

    [Fact]
    public void IndicatorMessages_NoPriorSuccessfulSave_ShowsNever()
    {
        var cashFlowRepository = new SyncStatusCashFlowRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null),
        };

        var vm = new SyncStatusViewModel(cashFlowRepository, new SyncStatusRepositoryStub());

        vm.IndicatorMessages.Should().ContainSingle().Which.Should().Contain("Last successful save: Never.");
    }

    [Fact]
    public void IsIndicatorVisible_AndIndicatorMessages_UpdateAfterRefreshStatus()
    {
        var cashFlowRepository = new SyncStatusCashFlowRepositoryStub
        {
            StatusToReturn = new SyncStatus(SyncState.Failed, "Drive unreachable", null),
        };
        var vm = new SyncStatusViewModel(cashFlowRepository, new SyncStatusRepositoryStub());
        vm.IsIndicatorVisible.Should().BeTrue();

        cashFlowRepository.StatusToReturn = new SyncStatus(SyncState.Idle, null, DateTime.UtcNow);
        vm.RefreshStatus();

        vm.IsIndicatorVisible.Should().BeFalse();
        vm.IndicatorMessages.Should().BeEmpty();
    }
}
