using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.ViewModels.Investment.Dashboard;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Investment.Dashboard;

public class DashboardKpiTilesViewModelTests
{
    private static PortfolioDashboardDTO Summary() => new()
    {
        MarketValue = 1000m,
        Invested = 800m,
        UnrealisedGainLoss = 200m,
        RealisedGainLoss = -50m,
        IncomeYtd = 30m,
        IncomeLifetime = 120m,
        GrossXirr = 0.12m,
        NetXirr = 0.09m,
    };

    private static (DashboardKpiTilesViewModel ViewModel, StubPortfolioDashboardService Service) CreateViewModel(
        StubPortfolioDashboardService? service = null)
    {
        service ??= new StubPortfolioDashboardService();
        var viewModel = new DashboardKpiTilesViewModel(service, new RecordingLogger<DashboardKpiTilesViewModel>());
        return (viewModel, service);
    }

    [Fact]
    public async Task LoadAsync_PopulatesTheEightNativeTiles()
    {
        var service = new StubPortfolioDashboardService { Dashboard = Summary() };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();

        vm.MarketValue.Should().Be(1000m);
        vm.Invested.Should().Be(800m);
        vm.UnrealisedGainLoss.Should().Be(200m);
        vm.RealisedGainLoss.Should().Be(-50m);
        vm.IncomeYtd.Should().Be(30m);
        vm.IncomeLifetime.Should().Be(120m);
        vm.GrossXirr.Should().Be(0.12m);
        vm.NetXirr.Should().Be(0.09m);
        vm.ShowContent.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_PopulatesTheEightConvertedTilesWhenReportingCurrencyIsEnabled()
    {
        var service = new StubPortfolioDashboardService
        {
            Dashboard = new PortfolioDashboardDTO
            {
                ReportingCurrency = "GBP",
                IsReportingCurrencyEnabled = true,
                ConvertedMarketValue = 900m,
                ConvertedInvested = 700m,
                ConvertedUnrealisedGainLoss = 200m,
                ConvertedRealisedGainLoss = -40m,
                ConvertedIncomeYtd = 25m,
                ConvertedIncomeLifetime = 100m,
                ConvertedGrossXirr = 0.11m,
                ConvertedNetXirr = 0.08m,
            },
        };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();

        vm.ReportingCurrency.Should().Be("GBP");
        vm.ShowConvertedTotals.Should().BeTrue();
        vm.ConvertedMarketValue.Should().Be(900m);
        vm.ConvertedInvested.Should().Be(700m);
        vm.ConvertedUnrealisedGainLoss.Should().Be(200m);
        vm.ConvertedRealisedGainLoss.Should().Be(-40m);
        vm.ConvertedIncomeYtd.Should().Be(25m);
        vm.ConvertedIncomeLifetime.Should().Be(100m);
        vm.ConvertedGrossXirr.Should().Be(0.11m);
        vm.ConvertedNetXirr.Should().Be(0.08m);
    }

    [Fact]
    public async Task LoadAsync_IsLoadingWhileTheServiceCallIsInFlightAndShowsContentAfterwards()
    {
        var gate = new TaskCompletionSource<PortfolioDashboardDTO>();
        var service = new StubPortfolioDashboardService { Gate = gate };
        var (vm, _) = CreateViewModel(service);

        var load = vm.LoadAsync();

        vm.IsLoading.Should().BeTrue();
        vm.ShowContent.Should().BeFalse();

        gate.SetResult(Summary());
        await load;

        vm.IsLoading.Should().BeFalse();
        vm.ShowContent.Should().BeTrue();
        vm.MarketValue.Should().Be(1000m);
    }

    [Fact]
    public async Task LoadAsync_FailureSurfacesErrorMessageAndRetryRecovers()
    {
        var service = new StubPortfolioDashboardService { ThrowOnGetDashboard = new InvalidOperationException("boom") };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();

        vm.ErrorMessage.Should().Be("boom");
        vm.HasError.Should().BeTrue();
        vm.ShowContent.Should().BeFalse();

        service.ThrowOnGetDashboard = null;
        service.Dashboard = Summary();
        await vm.LoadAsync();

        vm.ErrorMessage.Should().BeNull();
        vm.ShowContent.Should().BeTrue();
        vm.MarketValue.Should().Be(1000m);
    }

    [Theory]
    [InlineData(1, "1 holding could not be valued; Market Value, Unrealised Gain/Loss and both XIRR figures are incomplete.")]
    [InlineData(3, "3 holdings could not be valued; Market Value, Unrealised Gain/Loss and both XIRR figures are incomplete.")]
    public async Task LoadAsync_PartialSummaryProducesTheUnvaluedHoldingsNotice(int count, string expected)
    {
        var service = new StubPortfolioDashboardService
        {
            Dashboard = new PortfolioDashboardDTO { IsPartial = true, UnvaluedHoldingCount = count },
        };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();

        vm.IsPartial.Should().BeTrue();
        vm.UnvaluedHoldingCount.Should().Be(count);
        vm.PartialNoticeText.Should().Be(expected);
        vm.ShowPartialNotice.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_CompleteSummaryShowsNoPartialNotice()
    {
        var service = new StubPortfolioDashboardService { Dashboard = Summary() };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();

        vm.PartialNoticeText.Should().BeNull();
        vm.ShowPartialNotice.Should().BeFalse();
    }

    [Fact]
    public void ViewMissingPriceHoldingsCommand_RaisesExpandMissingPriceRequested()
    {
        var (vm, _) = CreateViewModel();
        var raised = 0;
        vm.ExpandMissingPriceRequested += (_, _) => raised++;

        vm.ViewMissingPriceHoldingsCommand.Execute(null);

        raised.Should().Be(1);
    }

    [Fact]
    public async Task ReportingCurrencyDisabled_HidesBothTheConvertedTotalsAndTheUnavailableMessage()
    {
        var service = new StubPortfolioDashboardService { Dashboard = Summary() };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();

        vm.IsReportingCurrencyEnabled.Should().BeFalse();
        vm.ShowConvertedTotals.Should().BeFalse();
        vm.IsReportingCurrencyUnavailable.Should().BeFalse();
        vm.IsReportingCurrencyPartial.Should().BeFalse();
    }

    [Fact]
    public async Task ReportingCurrencyUnavailable_ReplacesTheConvertedTotals()
    {
        var service = new StubPortfolioDashboardService
        {
            Dashboard = new PortfolioDashboardDTO
            {
                ReportingCurrency = "GBP",
                IsReportingCurrencyEnabled = true,
                IsReportingCurrencyUnavailable = true,
            },
        };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();

        vm.IsReportingCurrencyUnavailable.Should().BeTrue();
        vm.ShowConvertedTotals.Should().BeFalse();
        vm.IsReportingCurrencyPartial.Should().BeFalse();
    }

    [Fact]
    public async Task ReportingCurrencyPartial_ShowsTheConvertedTotalsWithTheirPartialStatus()
    {
        var service = new StubPortfolioDashboardService
        {
            Dashboard = new PortfolioDashboardDTO
            {
                ReportingCurrency = "GBP",
                IsReportingCurrencyEnabled = true,
                IsReportingCurrencyPartial = true,
                ConvertedMarketValue = 900m,
            },
        };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();

        vm.ShowConvertedTotals.Should().BeTrue();
        vm.IsReportingCurrencyPartial.Should().BeTrue();
        vm.IsReportingCurrencyUnavailable.Should().BeFalse();
    }

    [Fact]
    public void Constructor_RejectsMissingDependencies()
    {
        var act = () => new DashboardKpiTilesViewModel(null!, new RecordingLogger<DashboardKpiTilesViewModel>());

        act.Should().Throw<ArgumentNullException>();
    }
}

internal sealed class StubPortfolioDashboardService : IPortfolioDashboardService
{
    public PortfolioDashboardDTO Dashboard { get; set; } = new();

    public Exception? ThrowOnGetDashboard { get; set; }

    public TaskCompletionSource<PortfolioDashboardDTO>? Gate { get; set; }

    public int GetDashboardCallCount { get; private set; }

    public async Task<PortfolioDashboardDTO> GetDashboardAsync()
    {
        GetDashboardCallCount++;

        if (Gate is not null)
        {
            return await Gate.Task;
        }

        if (ThrowOnGetDashboard is not null)
        {
            throw ThrowOnGetDashboard;
        }

        return Dashboard;
    }
}
