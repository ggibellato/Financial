using Financial.Application.DTOs;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class PortfolioAssetSummaryRowViewModelTests
{
    private static PortfolioAssetSummaryRowViewModel BuildRow(
        decimal currentQuantity = 25m,
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
        decimal currentMonthCredits = 0m)
    {
        var dto = new PortfolioAssetSummaryItemDTO
        {
            AssetName = "Test Asset",
            Ticker = "TST",
            Exchange = "LSE",
            FirstInvestmentDate = firstInvestmentDate,
            CurrentQuantity = currentQuantity,
            TotalBought = totalInvested,
            TotalSold = 0m,
            TotalInvested = totalInvested,
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
        return new PortfolioAssetSummaryRowViewModel(dto);
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
        var row = new PortfolioAssetSummaryRowViewModel(dto);
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
        var row = BuildRow(currentQuantity: 25m, totalInvested: 250m);
        row.ApplyPrice(10.50m);
        // CurrentValue = 262.50, Profit = (262.50 - 250) / 250 * 100 = 5.00
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
        var row = BuildRow(currentQuantity: 25m, totalInvested: 250m, totalCredits: 12.5m);
        row.ApplyPrice(10.50m);
        // CurrentValue = 262.50, ProfitWithCredits = (262.50 + 12.5 - 250) / 250 * 100 = 10.00
        row.DisplayProfitWithCreditsPercent.Should().Be("10.00%");
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
    public void DisplayXirr_WhenSingleCashFlow_ReturnsNonDashValue()
    {
        // One buy entry + terminal = 2 entries → should converge
        var buyDate = DateTime.Today.AddYears(-2);
        var cashFlows = new List<AssetCashFlowDTO>
        {
            new() { Date = buyDate, Amount = -1000m }
        };
        var row = BuildRow(currentQuantity: 1m, totalInvested: 1000m, cashFlows: cashFlows);
        row.ApplyPrice(1210m); // CurrentValue = 1210
        row.DisplayXirr.Should().NotBe("—");
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
        var row = BuildRow(currentQuantity: 25m, totalInvested: 250m);
        row.ApplyPrice(10.50m); // CurrentValue = 262.50 > 250
        row.ProfitIsPositive.Should().BeTrue();
        row.ProfitIsNegative.Should().BeFalse();
    }

    [Fact]
    public void ProfitIsNegative_WhenCurrentValueBelowTotalInvested_IsTrue()
    {
        var row = BuildRow(currentQuantity: 25m, totalInvested: 300m);
        row.ApplyPrice(10.00m); // CurrentValue = 250 < 300
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
        // CurrentValue < TotalInvested but CurrentValue + TotalCredits > TotalInvested
        var row = BuildRow(currentQuantity: 25m, totalInvested: 300m, totalCredits: 100m);
        row.ApplyPrice(10.00m); // CurrentValue = 250, 250 + 100 = 350 > 300
        row.ProfitWithCreditsIsPositive.Should().BeTrue();
        row.ProfitWithCreditsIsNegative.Should().BeFalse();
    }

    [Fact]
    public void ProfitWithCreditsIsNegative_WhenProfitWithCreditsBelowZero_IsTrue()
    {
        var row = BuildRow(currentQuantity: 25m, totalInvested: 300m, totalCredits: 0m);
        row.ApplyPrice(10.00m); // CurrentValue = 250 < 300, no credits
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

        raised.Should().Contain(nameof(row.DisplayCurrentValue));
        raised.Should().Contain(nameof(row.DisplayProfitPercent));
        raised.Should().Contain(nameof(row.DisplayProfitWithCreditsPercent));
        raised.Should().Contain(nameof(row.DisplayXirr));
        raised.Should().Contain(nameof(row.PriceFetchFailed));
        raised.Should().Contain(nameof(row.IsLoadingPrice));
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
}
