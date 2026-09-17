using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Investment.Dashboard;
using Financial.TestUtilities;
using FluentAssertions;
using OxyPlot.Series;

namespace Financial.Presentation.Tests.ViewModels.Investment.Dashboard;

public class AllocationBreakdownViewModelTests
{
    private static AllocationBreakdownDTO Breakdown() => new()
    {
        ByClass =
        [
            new AssetClassAllocationEntryDTO(GlobalAssetClass.Equity, 600m, 60m),
            new AssetClassAllocationEntryDTO(GlobalAssetClass.Bond, 400m, 40m),
        ],
        ByCurrency =
        [
            new CurrencyAllocationEntryDTO("GBP", 700m, 70m),
            new CurrencyAllocationEntryDTO("BRL", 300m, 30m),
        ],
        ByCountry =
        [
            new CountryAllocationEntryDTO(CountryCode.UK, 1000m, 100m),
        ],
        ByBroker =
        [
            new BrokerAllocationEntryDTO("Trading212", 550m, 55m),
            new BrokerAllocationEntryDTO("Chase", 450m, 45m),
        ],
    };

    private static (AllocationBreakdownViewModel ViewModel, StubAllocationBreakdownService Service) CreateViewModel(
        StubAllocationBreakdownService? service = null)
    {
        service ??= new StubAllocationBreakdownService { Breakdown = Breakdown() };
        var viewModel = new AllocationBreakdownViewModel(service, new RecordingLogger<AllocationBreakdownViewModel>());
        return (viewModel, service);
    }

    [Fact]
    public async Task LoadAsync_DefaultsToTheClassDimension()
    {
        var (vm, _) = CreateViewModel();

        await vm.LoadAsync();

        vm.SelectedDimension.Should().Be(AllocationDimension.Class);
        vm.ChartTitle.Should().Be("Allocation by asset class");
        vm.Entries.Select(entry => entry.Label).Should().Equal("Equity", "Bond");
        vm.Entries[0].MarketValue.Should().Be(600m);
        vm.Entries[0].Percentage.Should().Be(60m);
        vm.ShowContent.Should().BeTrue();
        vm.ShowChart.Should().BeTrue();
        vm.IsEmpty.Should().BeFalse();
    }

    [Theory]
    [InlineData(AllocationDimension.Currency, "Allocation by currency", "GBP", "BRL")]
    [InlineData(AllocationDimension.Country, "Allocation by country", "UK")]
    [InlineData(AllocationDimension.Broker, "Allocation by broker", "Trading212", "Chase")]
    public async Task SelectedDimension_SwapsTheLegendRowsAndRebuildsThePlotModel(
        AllocationDimension dimension, string expectedTitle, params string[] expectedLabels)
    {
        var (vm, service) = CreateViewModel();
        await vm.LoadAsync();
        var classModel = vm.PlotModel;

        vm.SelectedDimension = dimension;

        vm.ChartTitle.Should().Be(expectedTitle);
        vm.Entries.Select(entry => entry.Label).Should().Equal(expectedLabels);
        vm.PlotModel.Should().NotBeSameAs(classModel);
        SliceCount(vm).Should().Be(expectedLabels.Length);
        service.GetAllocationBreakdownCallCount.Should().Be(1);
    }

    [Fact]
    public async Task LoadAsync_PreservesTheBackendOrderWithNoClientSideResort()
    {
        var service = new StubAllocationBreakdownService
        {
            Breakdown = new AllocationBreakdownDTO
            {
                ByBroker =
                [
                    new BrokerAllocationEntryDTO("Zeta", 900m, 90m),
                    new BrokerAllocationEntryDTO("Alpha", 100m, 10m),
                ],
            },
        };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();
        vm.SelectedDimension = AllocationDimension.Broker;

        vm.Entries.Select(entry => entry.Label).Should().Equal("Zeta", "Alpha");
    }

    [Fact]
    public async Task EmptyDimension_SetsTheEmptyStateAndBuildsAnEmptyPlotModel()
    {
        var (vm, _) = CreateViewModel(new StubAllocationBreakdownService { Breakdown = new AllocationBreakdownDTO() });

        await vm.LoadAsync();

        vm.IsEmpty.Should().BeTrue();
        vm.ShowChart.Should().BeFalse();
        vm.ShowContent.Should().BeTrue();
        vm.PlotModel.Series.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_FailureSurfacesErrorMessageAndRetryRecovers()
    {
        var service = new StubAllocationBreakdownService
        {
            ThrowOnGetAllocationBreakdown = new InvalidOperationException("boom"),
        };
        var (vm, _) = CreateViewModel(service);

        await vm.LoadAsync();

        vm.ErrorMessage.Should().Be("boom");
        vm.HasError.Should().BeTrue();
        vm.ShowContent.Should().BeFalse();
        vm.ShowChart.Should().BeFalse();

        service.ThrowOnGetAllocationBreakdown = null;
        service.Breakdown = Breakdown();
        await vm.LoadAsync();

        vm.ErrorMessage.Should().BeNull();
        vm.ShowContent.Should().BeTrue();
        vm.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task LegendRows_ExposeNoCommandOrSelectionMember()
    {
        var (vm, _) = CreateViewModel();
        await vm.LoadAsync();

        var members = typeof(AllocationEntryRowViewModel)
            .GetProperties()
            .Select(property => property.Name);

        members.Should().BeEquivalentTo(
            nameof(AllocationEntryRowViewModel.Label),
            nameof(AllocationEntryRowViewModel.MarketValue),
            nameof(AllocationEntryRowViewModel.Percentage));
    }

    [Fact]
    public void Constructor_RejectsMissingDependencies()
    {
        var act = () => new AllocationBreakdownViewModel(null!, new RecordingLogger<AllocationBreakdownViewModel>());

        act.Should().Throw<ArgumentNullException>();
    }

    private static int SliceCount(AllocationBreakdownViewModel viewModel) =>
        viewModel.PlotModel.Series.OfType<PieSeries>().Single().Slices.Count;
}

internal sealed class StubAllocationBreakdownService : IAllocationBreakdownService
{
    public AllocationBreakdownDTO Breakdown { get; set; } = new();

    public Exception? ThrowOnGetAllocationBreakdown { get; set; }

    public int GetAllocationBreakdownCallCount { get; private set; }

    public AllocationBreakdownDTO GetAllocationBreakdown()
    {
        GetAllocationBreakdownCallCount++;

        if (ThrowOnGetAllocationBreakdown is not null)
        {
            throw ThrowOnGetAllocationBreakdown;
        }

        return Breakdown;
    }
}
