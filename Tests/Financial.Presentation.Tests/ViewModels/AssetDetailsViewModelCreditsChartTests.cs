using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
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
            new StubBrokerBreakdownService(),
            new StubTransactionQueryService(),
            new XirrCalculationService(),
            new ProfitCalculationService());
    }

    [Fact]
    public void SetCreditsChartType_DefaultsToBar()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);
        vm.Credits.CreditsChartTypes.First(t => t.Value == CreditsChartType.Bar).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SetCreditsChartType_PersistsSelectionPerNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.Credits.SelectCreditsChartTypeCommand.Execute(CreditsChartType.Line);
        vm.Credits.CreditsChartTypes.First(t => t.Value == CreditsChartType.Line).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerB", new AggregatedSummaryDTO(), []);
        vm.Credits.CreditsChartTypes.First(t => t.Value == CreditsChartType.Bar).IsSelected.Should().BeTrue();

        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);
        vm.Credits.CreditsChartTypes.First(t => t.Value == CreditsChartType.Line).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SetCreditsChartType_DoesNotAffectSelectedTypeMode_ForSameNode()
    {
        var vm = BuildViewModel();
        vm.LoadBrokerSummary("BrokerA", new AggregatedSummaryDTO(), []);

        vm.Credits.SelectCreditsChartTypeCommand.Execute(CreditsChartType.Line);

        vm.Credits.CreditsTypeModes.First(m => m.Value == CreditsTypeChartMode.Stacked).IsSelected.Should().BeTrue();
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
        vm.Credits.CreditsPlotModel.Should().NotBeNull();
        var barSeriesCount = vm.Credits.CreditsPlotModel!.Series.Count;

        vm.Credits.SelectCreditsChartTypeCommand.Execute(CreditsChartType.Line);

        vm.Credits.CreditsPlotModel.Should().NotBeNull();
        vm.Credits.CreditsPlotModel!.Series.Should().HaveCount(barSeriesCount);
    }

}
