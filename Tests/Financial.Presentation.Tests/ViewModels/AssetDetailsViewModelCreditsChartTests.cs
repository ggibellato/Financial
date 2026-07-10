using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetDetailsViewModelCreditsChartTests
{
    private static AssetDetailsViewModel BuildViewModel()
    {
        return new AssetDetailsViewModel(
            new StubTransactionService(),
            new StubCreditService(),
            new StubAssetPriceService(),
            new StubBrokerBreakdownQueryService(),
            new StubTransactionQueryService());
    }

    [Fact]
    public void SetCreditsChartType_DefaultsToBar()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        vm.CreditsChartTypes.First(t => t.ChartType == CreditsChartType.Bar).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SetCreditsChartType_PersistsSelectionPerNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.SelectCreditsChartTypeCommand.Execute(CreditsChartType.Line);
        vm.CreditsChartTypes.First(t => t.ChartType == CreditsChartType.Line).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerB", new AggregatedSummaryDTO(), []);
        vm.CreditsChartTypes.First(t => t.ChartType == CreditsChartType.Bar).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);
        vm.CreditsChartTypes.First(t => t.ChartType == CreditsChartType.Line).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SetCreditsChartType_DoesNotAffectSelectedTypeMode_ForSameNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.SelectCreditsChartTypeCommand.Execute(CreditsChartType.Line);

        vm.CreditsTypeModes.First(m => m.Mode == CreditsTypeChartMode.Stacked).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SetCreditsChartType_RebuildsCreditsPlotModel()
    {
        var credits = new List<CreditDTO>
        {
            new() { Id = Guid.NewGuid(), Date = DateTime.Today, Type = "Dividend", Value = 100m },
        };
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), credits);
        vm.CreditsPlotModel.Should().NotBeNull();
        var barSeriesCount = vm.CreditsPlotModel!.Series.Count;

        vm.SelectCreditsChartTypeCommand.Execute(CreditsChartType.Line);

        vm.CreditsPlotModel.Should().NotBeNull();
        vm.CreditsPlotModel!.Series.Should().HaveCount(barSeriesCount);
    }

}
