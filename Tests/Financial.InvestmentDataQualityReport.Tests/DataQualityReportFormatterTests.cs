using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.DataQualityReport;
using FluentAssertions;

namespace Financial.InvestmentDataQualityReport.Tests;

public class DataQualityReportFormatterTests
{
    [Fact]
    public void Format_EmptyReport_NamesEveryCategoryAsZero()
    {
        var report = new DataQualityReportDTO();

        var output = DataQualityReportFormatter.Format(report);

        output.Should().Contain("Holdings selling more than they hold (0)");
        output.Should().Contain("Open holdings with no recorded price (0)");
        output.Should().Contain("Unclassified holdings (0: 0 active, 0 historic)");
        output.Should().Contain("Unclassified holdings that are also unpriced (0)");
        output.Should().Contain("Historic holdings still carrying a quantity (0)");
    }

    [Fact]
    public void Format_SalesExceedPurchases_NamesHoldingAndShortfall()
    {
        var report = new DataQualityReportDTO
        {
            SalesExceedPurchases =
            [
                new SalesExceedPurchasesFinding("XPI", "Default", "OVERSOLD", new DateTime(2021, 6, 1), 5m, 3m),
            ],
        };

        var output = DataQualityReportFormatter.Format(report);

        output.Should().Contain("Holdings selling more than they hold (1)");
        output.Should().Contain("XPI / Default / OVERSOLD");
        output.Should().Contain("2021-06-01");
        output.Should().Contain("3");
        output.Should().Contain("5");
    }

    [Fact]
    public void Format_UnpricedOpenHoldings_NamesEachHolding()
    {
        var report = new DataQualityReportDTO
        {
            UnpricedOpenHoldings = [new UnpricedOpenHoldingFinding("XPI", "Default", "UNPRICED")],
        };

        var output = DataQualityReportFormatter.Format(report);

        output.Should().Contain("Open holdings with no recorded price (1)");
        output.Should().Contain("XPI / Default / UNPRICED");
    }

    [Fact]
    public void Format_UnclassifiedHoldings_SplitsActiveAndHistoricCounts()
    {
        var report = new DataQualityReportDTO
        {
            UnclassifiedHoldings =
            [
                new UnclassifiedHoldingFinding("XPI", "Default", "ACTIVEONE", InvestmentScope.Active),
                new UnclassifiedHoldingFinding("XPI", "Closed", "HISTORICONE", InvestmentScope.Historic),
                new UnclassifiedHoldingFinding("XPI", "Closed", "HISTORICTWO", InvestmentScope.Historic),
            ],
        };

        var output = DataQualityReportFormatter.Format(report);

        output.Should().Contain("Unclassified holdings (3: 1 active, 2 historic)");
        output.Should().Contain("ACTIVEONE");
        output.Should().Contain("HISTORICONE");
        output.Should().Contain("HISTORICTWO");
    }

    [Fact]
    public void Format_UnclassifiedAndUnpricedLink_NamesTheOverlap()
    {
        var report = new DataQualityReportDTO
        {
            UnclassifiedAndUnpricedOpenHoldings = [new UnclassifiedHoldingFinding("XPI", "Default", "BOND", InvestmentScope.Active)],
        };

        var output = DataQualityReportFormatter.Format(report);

        output.Should().Contain("Unclassified holdings that are also unpriced (1)");
        output.Should().Contain("BOND");
        output.ToLowerInvariant().Should().Contain("classifying it may also fix its price");
    }

    [Fact]
    public void Format_HistoricHoldingsStillOpen_NamesQuantityAndCost()
    {
        var report = new DataQualityReportDTO
        {
            HistoricHoldingsStillOpen = [new HistoricHoldingStillOpenFinding("XPI", "Closed", "STILLOPEN", 28m, 2100m)],
        };

        var output = DataQualityReportFormatter.Format(report);

        output.Should().Contain("Historic holdings still carrying a quantity (1)");
        output.Should().Contain("STILLOPEN");
        output.Should().Contain("28");
        output.Should().Contain("2100");
    }
}
