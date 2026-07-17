using Financial.Application.DTOs;
using Financial.Application.Services;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class PortfolioAssetSummaryRowViewModelTests
{
    private static PortfolioAssetSummaryRowViewModel BuildRow(
        decimal currentQuantity = 25m,
        decimal averagePrice = 0m,
        decimal totalInvested = 250m,
        decimal portfolioWeight = 0m,
        decimal totalCredits = 0m,
        DateTime? firstInvestmentDate = null,
        IReadOnlyList<AssetCashFlowDTO>? cashFlows = null,
        decimal lastMonthCredits = 0m,
        string? lastCreditMonth = null,
        decimal? lastMonthCreditsPercent = null,
        decimal? estimatedAnnualCredits = null,
        decimal? estimatedAnnualPercent = null,
        decimal currentMonthCredits = 0m,
        decimal? totalBought = null,
        decimal realizedGainLoss = 0m,
        decimal? averageSellPrice = null)
    {
        var dto = new PortfolioAssetSummaryItemDTO
        {
            AssetName = "Test Asset",
            Ticker = "TST",
            Exchange = "LSE",
            FirstInvestmentDate = firstInvestmentDate,
            CurrentQuantity = currentQuantity,
            AveragePrice = averagePrice,
            AverageSellPrice = averageSellPrice,
            TotalBought = totalBought ?? totalInvested,
            TotalSold = 0m,
            TotalInvested = totalInvested,
            RealizedGainLoss = realizedGainLoss,
            PortfolioWeight = portfolioWeight,
            TotalCredits = totalCredits,
            CashFlows = cashFlows ?? [],
            LastMonthCredits = lastMonthCredits,
            LastCreditMonth = lastCreditMonth,
            LastMonthCreditsPercent = lastMonthCreditsPercent,
            EstimatedAnnualCredits = estimatedAnnualCredits,
            EstimatedAnnualPercent = estimatedAnnualPercent,
            CurrentMonthCredits = currentMonthCredits
        };
        return new PortfolioAssetSummaryRowViewModel(dto, new XirrCalculationService(), new ProfitCalculationService());
    }

    [Fact]
    public void DisplayFirstInvestmentDate_WhenDateIsSet_ReturnsShortDateString()
    {
        var row = BuildRow(firstInvestmentDate: new DateTime(2021, 3, 1));
        row.DisplayFirstInvestmentDate.Should().Be("01/03/2021");
    }

    [Fact]
    public void DisplayFirstInvestmentDate_WhenDateIsNull_ReturnsEmptyString()
    {
        var row = BuildRow(firstInvestmentDate: null);
        row.DisplayFirstInvestmentDate.Should().Be(string.Empty);
    }

    [Fact]
    public void DisplayCurrentQuantity_FormatsN8()
    {
        var row = BuildRow(currentQuantity: 25.0m);
        row.DisplayCurrentQuantity.Should().Be("25.00000000");
    }

    [Fact]
    public void DisplayAveragePrice_FormatsN2()
    {
        var row = BuildRow(averagePrice: 100.5m);
        row.DisplayAveragePrice.Should().Be("100.50");
    }

    [Fact]
    public void DisplayTotalInvested_FormatsN2()
    {
        var row = BuildRow(totalInvested: 2500.5m);
        row.DisplayTotalInvested.Should().Be("2,500.50");
    }

    [Fact]
    public void DisplayPortfolioWeight_FormatsOneDecimalPercent()
    {
        var dto = new PortfolioAssetSummaryItemDTO
        {
            AssetName = "Test", Ticker = "T", Exchange = "E",
            CurrentQuantity = 1m, TotalBought = 1m, TotalSold = 0m,
            TotalInvested = 1m, PortfolioWeight = 23.4567m
        };
        var row = new PortfolioAssetSummaryRowViewModel(dto, new XirrCalculationService(), new ProfitCalculationService());
        row.DisplayPortfolioWeight.Should().Be("23.5%");
    }

    [Fact]
    public void DisplayTotalCredits_FormatsN2()
    {
        var row = BuildRow(totalCredits: 150.75m);
        row.DisplayTotalCredits.Should().Be("150.75");
    }

    [Fact]
    public void DisplayCurrentValue_WhenIsLoadingPrice_ReturnsDash()
    {
        var row = BuildRow();
        row.IsLoadingPrice.Should().BeTrue();
        row.DisplayCurrentValue.Should().Be("—");
    }

    [Fact]
    public void DisplayCurrentValue_WhenPriceFetchFailed_ReturnsDash()
    {
        var row = BuildRow();
        row.MarkPriceFailed();
        row.DisplayCurrentValue.Should().Be("—");
    }

    [Fact]
    public void DisplayCurrentValue_AfterApplyPrice_ReturnsComputedValueN2()
    {
        var row = BuildRow(currentQuantity: 25m);
        row.ApplyPrice(10.50m);
        row.DisplayCurrentValue.Should().Be("262.50");
    }

    [Fact]
    public void DisplayCurrentPrice_WhenIsLoadingPrice_ReturnsDash()
    {
        var row = BuildRow();
        row.IsLoadingPrice.Should().BeTrue();
        row.DisplayCurrentPrice.Should().Be("—");
    }

    [Fact]
    public void DisplayCurrentPrice_WhenPriceFetchFailed_ReturnsDash()
    {
        var row = BuildRow();
        row.MarkPriceFailed();
        row.DisplayCurrentPrice.Should().Be("—");
    }

    [Fact]
    public void DisplayCurrentPrice_AfterApplyPrice_ReturnsPriceN2()
    {
        var row = BuildRow();
        row.ApplyPrice(10.50m);
        row.DisplayCurrentPrice.Should().Be("10.50");
    }

    [Fact]
    public void DisplayProfitPercent_WhenIsLoadingPrice_ReturnsDash()
    {
        var row = BuildRow();
        row.IsLoadingPrice.Should().BeTrue();
        row.DisplayProfitPercent.Should().Be("—");
    }

    [Fact]
    public void DisplayProfitPercent_WhenPriceFetchFailed_ReturnsDash()
    {
        var row = BuildRow();
        row.MarkPriceFailed();
        row.DisplayProfitPercent.Should().Be("—");
    }

    [Fact]
    public void DisplayProfitPercent_WhenTotalInvestedIsZero_ReturnsDash()
    {
        var row = BuildRow(totalInvested: 0m);
        row.ApplyPrice(10.50m);
        row.DisplayProfitPercent.Should().Be("—");
    }

    [Fact]
    public void DisplayProfitPercent_AfterApplyPrice_ReturnsFormattedPercent()
    {
        var row = BuildRow(currentQuantity: 25m, averagePrice: 10m, totalInvested: 250m);
        row.ApplyPrice(10.50m);
        // CurrentValue = 262.50, costBasis = 25 x 10 = 250, Profit = (262.50 - 250) / 250 * 100 = 5.00
        row.DisplayProfitPercent.Should().Be("5.00%");
    }

    [Fact]
    public void DisplayProfitWithCreditsPercent_WhenIsLoadingPrice_ReturnsDash()
    {
        var row = BuildRow();
        row.IsLoadingPrice.Should().BeTrue();
        row.DisplayProfitWithCreditsPercent.Should().Be("—");
    }

    [Fact]
    public void DisplayProfitWithCreditsPercent_WhenPriceFetchFailed_ReturnsDash()
    {
        var row = BuildRow();
        row.MarkPriceFailed();
        row.DisplayProfitWithCreditsPercent.Should().Be("—");
    }

    [Fact]
    public void DisplayProfitWithCreditsPercent_WhenTotalInvestedIsZero_ReturnsDash()
    {
        var row = BuildRow(totalInvested: 0m, totalCredits: 50m);
        row.ApplyPrice(10.50m);
        row.DisplayProfitWithCreditsPercent.Should().Be("—");
    }

    [Fact]
    public void DisplayProfitWithCreditsPercent_AfterApplyPrice_ReturnsFormattedPercent()
    {
        var row = BuildRow(currentQuantity: 25m, averagePrice: 10m, totalInvested: 250m, totalCredits: 12.5m);
        row.ApplyPrice(10.50m);
        // CurrentValue = 262.50, costBasis = 25 x 10 = 250, ProfitWithCredits = (262.50 + 12.5 - 250) / 250 * 100 = 10.00
        row.DisplayProfitWithCreditsPercent.Should().Be("10.00%");
    }

    [Fact]
    public void DisplayProfitPercent_UsesCurrentCostBasisNotGrossTotalInvested()
    {
        // Partial sell scenario: totalInvested (2000, gross bought) no longer reflects the current
        // position's cost basis once some quantity has been sold — quantity x averagePrice (60 x 20 = 1200) does.
        var row = BuildRow(currentQuantity: 60m, averagePrice: 20m, totalInvested: 2000m);
        row.ApplyPrice(25m); // CurrentValue = 1500, costBasis = 1200, Profit = (1500 - 1200) / 1200 * 100 = 25.00
        row.DisplayProfitPercent.Should().Be("25.00%");
    }

    [Fact]
    public void DisplayXirr_WhenIsLoadingPrice_ReturnsDash()
    {
        var row = BuildRow();
        row.IsLoadingPrice.Should().BeTrue();
        row.DisplayXirr.Should().Be("—");
    }

    [Fact]
    public void DisplayXirr_WhenPriceFetchFailed_ReturnsDash()
    {
        var row = BuildRow();
        row.MarkPriceFailed();
        row.DisplayXirr.Should().Be("—");
    }

    [Fact]
    public void DisplayXirr_WhenCashFlowsEmpty_ReturnsDash()
    {
        // Empty cash flows + terminal = only 1 entry → fewer than 2
        var row = BuildRow(cashFlows: []);
        row.ApplyPrice(1000m);
        row.DisplayXirr.Should().Be("—");
    }

    [Fact]
    public void DisplayXirr_AfterApplyPrice_ReturnsConvergedValueN2Percent()
    {
        // One buy at -1000 exactly 2 years ago; terminal = +1210 today
        // Expected XIRR ≈ 10% per year: 1000 * (1.10)^2 = 1210
        var buyDate = new DateTime(DateTime.Today.Year - 2, DateTime.Today.Month, DateTime.Today.Day);
        var cashFlows = new List<AssetCashFlowDTO>
        {
            new() { Date = buyDate, Amount = -1000m }
        };
        var row = BuildRow(currentQuantity: 1m, totalInvested: 1000m, cashFlows: cashFlows);
        row.ApplyPrice(1210m);

        row.Xirr.Should().NotBeNull();
        row.Xirr!.Value.Should().BeApproximately(10m, 0.1m);
        row.DisplayXirr.Should().NotBe("—");
    }

    [Fact]
    public void ProfitIsPositive_WhenCurrentValueExceedsTotalInvested_IsTrue()
    {
        var row = BuildRow(currentQuantity: 25m, averagePrice: 10m, totalInvested: 250m);
        row.ApplyPrice(10.50m); // CurrentValue = 262.50 > costBasis (250)
        row.ProfitIsPositive.Should().BeTrue();
        row.ProfitIsNegative.Should().BeFalse();
    }

    [Fact]
    public void ProfitIsNegative_WhenCurrentValueBelowTotalInvested_IsTrue()
    {
        var row = BuildRow(currentQuantity: 25m, averagePrice: 12m, totalInvested: 300m);
        row.ApplyPrice(10.00m); // CurrentValue = 250 < costBasis (300)
        row.ProfitIsNegative.Should().BeTrue();
        row.ProfitIsPositive.Should().BeFalse();
    }

    [Fact]
    public void ProfitIsPositive_WhenPriceUnavailable_IsFalse()
    {
        var row = BuildRow();
        row.MarkPriceFailed();
        row.ProfitIsPositive.Should().BeFalse();
        row.ProfitIsNegative.Should().BeFalse();
    }

    [Fact]
    public void ProfitWithCreditsIsPositive_WhenProfitWithCreditsExceedsZero_IsTrue()
    {
        // CurrentValue < costBasis but CurrentValue + TotalCredits > costBasis
        var row = BuildRow(currentQuantity: 25m, averagePrice: 12m, totalInvested: 300m, totalCredits: 100m);
        row.ApplyPrice(10.00m); // CurrentValue = 250, 250 + 100 = 350 > costBasis (300)
        row.ProfitWithCreditsIsPositive.Should().BeTrue();
        row.ProfitWithCreditsIsNegative.Should().BeFalse();
    }

    [Fact]
    public void ProfitWithCreditsIsNegative_WhenProfitWithCreditsBelowZero_IsTrue()
    {
        var row = BuildRow(currentQuantity: 25m, averagePrice: 12m, totalInvested: 300m, totalCredits: 0m);
        row.ApplyPrice(10.00m); // CurrentValue = 250 < costBasis (300), no credits
        row.ProfitWithCreditsIsNegative.Should().BeTrue();
        row.ProfitWithCreditsIsPositive.Should().BeFalse();
    }

    [Fact]
    public void ProfitWithCreditsIsPositive_WhenPriceUnavailable_IsFalse()
    {
        var row = BuildRow(totalCredits: 100m);
        row.MarkPriceFailed();
        row.ProfitWithCreditsIsPositive.Should().BeFalse();
        row.ProfitWithCreditsIsNegative.Should().BeFalse();
    }

    [Fact]
    public void XirrIsPositive_WhenXirrConvergesPositive_IsTrue()
    {
        var buyDate = DateTime.Today.AddYears(-2);
        var cashFlows = new List<AssetCashFlowDTO>
        {
            new() { Date = buyDate, Amount = -1000m }
        };
        var row = BuildRow(currentQuantity: 1m, totalInvested: 1000m, cashFlows: cashFlows);
        row.ApplyPrice(1210m); // positive XIRR ~10%
        row.XirrIsPositive.Should().BeTrue();
        row.XirrIsNegative.Should().BeFalse();
    }

    [Fact]
    public void XirrIsNegative_WhenXirrConvergesNegative_IsTrue()
    {
        // Buy at 1000, current value 500 → negative XIRR
        var buyDate = DateTime.Today.AddYears(-2);
        var cashFlows = new List<AssetCashFlowDTO>
        {
            new() { Date = buyDate, Amount = -1000m }
        };
        var row = BuildRow(currentQuantity: 1m, totalInvested: 1000m, cashFlows: cashFlows);
        row.ApplyPrice(500m);
        row.XirrIsNegative.Should().BeTrue();
        row.XirrIsPositive.Should().BeFalse();
    }

    [Fact]
    public void XirrIsPositive_WhenPriceUnavailable_IsFalse()
    {
        var row = BuildRow();
        row.MarkPriceFailed();
        row.XirrIsPositive.Should().BeFalse();
        row.XirrIsNegative.Should().BeFalse();
    }

    [Fact]
    public void ApplyPrice_RaisesPropertyChangedForDisplayProperties()
    {
        var row = BuildRow();
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        row.ApplyPrice(10.50m);

        raised.Should().Contain(nameof(row.DisplayCurrentPrice));
        raised.Should().Contain(nameof(row.DisplayCurrentValue));
        raised.Should().Contain(nameof(row.DisplayProfitPercent));
        raised.Should().Contain(nameof(row.DisplayProfitWithCreditsPercent));
        raised.Should().Contain(nameof(row.DisplayXirr));
        raised.Should().Contain(nameof(row.IsLoadingPrice));
        raised.Should().Contain(nameof(row.ProfitIsPositive));
        raised.Should().Contain(nameof(row.ProfitIsNegative));
        raised.Should().Contain(nameof(row.ProfitWithCreditsIsPositive));
        raised.Should().Contain(nameof(row.ProfitWithCreditsIsNegative));
        raised.Should().Contain(nameof(row.XirrIsPositive));
        raised.Should().Contain(nameof(row.XirrIsNegative));
    }

    [Fact]
    public void MarkPriceFailed_RaisesPropertyChangedForDisplayProperties()
    {
        var row = BuildRow();
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        row.MarkPriceFailed();

        raised.Should().Contain(nameof(row.DisplayCurrentPrice));
        raised.Should().Contain(nameof(row.DisplayCurrentValue));
        raised.Should().Contain(nameof(row.DisplayProfitPercent));
        raised.Should().Contain(nameof(row.DisplayProfitWithCreditsPercent));
        raised.Should().Contain(nameof(row.DisplayXirr));
        raised.Should().Contain(nameof(row.PriceFetchFailed));
        raised.Should().Contain(nameof(row.IsLoadingPrice));
    }

    [Fact]
    public void DisplayCurrentValue_AfterMarkPriceNotApplicable_ReturnsDash()
    {
        var row = BuildRow();
        row.MarkPriceNotApplicable();
        row.DisplayCurrentValue.Should().Be("—");
    }

    [Fact]
    public void DisplayXirr_AfterMarkPriceNotApplicable_ReturnsDash()
    {
        var row = BuildRow();
        row.MarkPriceNotApplicable();
        row.DisplayXirr.Should().Be("—");
    }

    [Fact]
    public void MarkPriceNotApplicable_DoesNotSetPriceFetchFailed()
    {
        var row = BuildRow();
        row.MarkPriceNotApplicable();
        row.PriceFetchFailed.Should().BeFalse();
        row.IsLoadingPrice.Should().BeFalse();
    }

    [Fact]
    public void MarkPriceNotApplicable_RaisesPropertyChangedForDisplayProperties()
    {
        var row = BuildRow();
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        row.MarkPriceNotApplicable();

        raised.Should().Contain(nameof(row.DisplayCurrentPrice));
        raised.Should().Contain(nameof(row.DisplayCurrentValue));
        raised.Should().Contain(nameof(row.DisplayProfitPercent));
        raised.Should().Contain(nameof(row.DisplayProfitWithCreditsPercent));
        raised.Should().Contain(nameof(row.DisplayXirr));
        raised.Should().Contain(nameof(row.IsLoadingPrice));
        raised.Should().NotContain(nameof(row.PriceFetchFailed));
    }

    [Fact]
    public void DisplayLastMonthCredits_WhenLastCreditMonthIsNotNull_FormatsN2()
    {
        var row = BuildRow(lastMonthCredits: 12.50m, lastCreditMonth: "2026-06");
        row.DisplayLastMonthCredits.Should().Be("12.50");
    }

    [Fact]
    public void DisplayLastMonthCredits_WhenLastCreditMonthIsNull_ReturnsDash()
    {
        var row = BuildRow(lastCreditMonth: null);
        row.DisplayLastMonthCredits.Should().Be("—");
    }

    [Fact]
    public void LastCreditMonthDisplay_WhenNotNull_FormatsAsMMMYyyy()
    {
        var row = BuildRow(lastCreditMonth: "2026-06");
        row.LastCreditMonthDisplay.Should().Be("Jun 2026");
    }

    [Fact]
    public void LastCreditMonthDisplay_WhenNull_ReturnsDash()
    {
        var row = BuildRow(lastCreditMonth: null);
        row.LastCreditMonthDisplay.Should().Be("—");
    }

    [Fact]
    public void DisplayLastMonthCreditsPercent_WhenNotNull_FormatsF2Percent()
    {
        var row = BuildRow(lastMonthCreditsPercent: 1.25m);
        row.DisplayLastMonthCreditsPercent.Should().Be("1.25%");
    }

    [Fact]
    public void DisplayLastMonthCreditsPercent_WhenNull_ReturnsDash()
    {
        var row = BuildRow(lastMonthCreditsPercent: null);
        row.DisplayLastMonthCreditsPercent.Should().Be("—");
    }

    [Fact]
    public void DisplayEstimatedAnnualCredits_WhenNotNull_FormatsN2()
    {
        var row = BuildRow(estimatedAnnualCredits: 150.00m);
        row.DisplayEstimatedAnnualCredits.Should().Be("150.00");
    }

    [Fact]
    public void DisplayEstimatedAnnualCredits_WhenNull_ReturnsDash()
    {
        var row = BuildRow(estimatedAnnualCredits: null);
        row.DisplayEstimatedAnnualCredits.Should().Be("—");
    }

    [Fact]
    public void DisplayEstimatedAnnualPercent_WhenNotNull_FormatsF2Percent()
    {
        var row = BuildRow(estimatedAnnualPercent: 6.00m);
        row.DisplayEstimatedAnnualPercent.Should().Be("6.00%");
    }

    [Fact]
    public void DisplayEstimatedAnnualPercent_WhenNull_ReturnsDash()
    {
        var row = BuildRow(estimatedAnnualPercent: null);
        row.DisplayEstimatedAnnualPercent.Should().Be("—");
    }

    [Fact]
    public void DisplayRealizedGainLoss_FormatsN2()
    {
        var row = BuildRow(realizedGainLoss: 123.456m);
        row.DisplayRealizedGainLoss.Should().Be("123.46");
    }

    [Fact]
    public void RealizedGainLossIsPositive_WhenGainPositive_IsTrue()
    {
        var row = BuildRow(realizedGainLoss: 100m);
        row.RealizedGainLossIsPositive.Should().BeTrue();
        row.RealizedGainLossIsNegative.Should().BeFalse();
    }

    [Fact]
    public void RealizedGainLossIsNegative_WhenLossNegative_IsTrue()
    {
        var row = BuildRow(realizedGainLoss: -50m);
        row.RealizedGainLossIsNegative.Should().BeTrue();
        row.RealizedGainLossIsPositive.Should().BeFalse();
    }

    [Fact]
    public void DisplaySoldPrice_WhenAverageSellPriceIsSet_FormatsN2()
    {
        var row = BuildRow(averageSellPrice: 12.5m);
        row.DisplaySoldPrice.Should().Be("12.50");
    }

    [Fact]
    public void DisplaySoldPrice_WhenAverageSellPriceIsNull_ReturnsDash()
    {
        var row = BuildRow(averageSellPrice: null);
        row.DisplaySoldPrice.Should().Be("—");
    }

    [Fact]
    public void DisplayHistoricProfitPercent_ExcludesCreditsFromRealizedGainLoss()
    {
        // Profit % reflects the realized capital gain alone: (200 - 50) / 1000 * 100 = 15.00
        var row = BuildRow(totalBought: 1000m, totalCredits: 50m, realizedGainLoss: 200m);
        row.HistoricProfitPercent.Should().Be(15.00m);
        row.DisplayHistoricProfitPercent.Should().Be("15.00%");
    }

    [Fact]
    public void DisplayHistoricProfitWithCreditsPercent_UsesFullRealizedGainLoss()
    {
        // Profit % w/ Credits uses the full realized gain/loss: 200 / 1000 * 100 = 20.00
        var row = BuildRow(totalBought: 1000m, totalCredits: 50m, realizedGainLoss: 200m);
        row.HistoricProfitWithCreditsPercent.Should().Be(20.00m);
        row.DisplayHistoricProfitWithCreditsPercent.Should().Be("20.00%");
    }

    [Fact]
    public void DisplayHistoricProfitPercent_WhenTotalBoughtIsZero_ReturnsDash()
    {
        var row = BuildRow(totalBought: 0m, realizedGainLoss: 100m);
        row.HistoricProfitPercent.Should().BeNull();
        row.DisplayHistoricProfitPercent.Should().Be("—");
    }

    [Fact]
    public void DisplayHistoricProfitWithCreditsPercent_WhenTotalBoughtIsZero_ReturnsDash()
    {
        var row = BuildRow(totalBought: 0m, realizedGainLoss: 100m);
        row.HistoricProfitWithCreditsPercent.Should().BeNull();
        row.DisplayHistoricProfitWithCreditsPercent.Should().Be("—");
    }

    [Fact]
    public void HistoricProfitIsPositive_WhenHistoricProfitPercentPositive_IsTrue()
    {
        var row = BuildRow(totalBought: 1000m, realizedGainLoss: 100m);
        row.HistoricProfitIsPositive.Should().BeTrue();
        row.HistoricProfitIsNegative.Should().BeFalse();
    }

    [Fact]
    public void HistoricProfitWithCreditsIsNegative_WhenHistoricProfitWithCreditsPercentNegative_IsTrue()
    {
        var row = BuildRow(totalBought: 1000m, realizedGainLoss: -100m);
        row.HistoricProfitWithCreditsIsNegative.Should().BeTrue();
        row.HistoricProfitWithCreditsIsPositive.Should().BeFalse();
    }

    [Fact]
    public void DisplayHistoricXirr_ComputesFromCashFlowsWithZeroTerminalValue()
    {
        // One buy at -1000 exactly 2 years ago; the position's proceeds are already recorded
        // as a +1210 cash flow today (fully realized) instead of a live terminal mark-to-market.
        var buyDate = DateTime.Today.AddYears(-2);
        var cashFlows = new List<AssetCashFlowDTO>
        {
            new() { Date = buyDate, Amount = -1000m },
            new() { Date = DateTime.Today, Amount = 1210m }
        };
        var row = BuildRow(cashFlows: cashFlows);

        row.HistoricXirr.Should().NotBeNull();
        row.HistoricXirr!.Value.Should().BeApproximately(10m, 0.1m);
        row.DisplayHistoricXirr.Should().NotBe("—");
        row.HistoricXirrIsPositive.Should().BeTrue();
    }

    [Fact]
    public void DisplayHistoricXirr_WhenCashFlowsEmpty_ReturnsDash()
    {
        var row = BuildRow(cashFlows: []);
        row.HistoricXirr.Should().BeNull();
        row.DisplayHistoricXirr.Should().Be("—");
    }
}
