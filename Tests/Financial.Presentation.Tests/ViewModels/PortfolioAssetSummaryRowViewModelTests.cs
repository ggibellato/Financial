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
        DateTime? firstInvestmentDate = null)
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
            PortfolioWeight = portfolioWeight
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
        row.DisplayProfitPercent.Should().Be("5.00%");
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
    public void ApplyPrice_RaisesPropertyChangedForDisplayProperties()
    {
        var row = BuildRow();
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        row.ApplyPrice(10.50m);

        raised.Should().Contain(nameof(row.DisplayCurrentValue));
        raised.Should().Contain(nameof(row.DisplayProfitPercent));
        raised.Should().Contain(nameof(row.IsLoadingPrice));
        raised.Should().Contain(nameof(row.ProfitIsPositive));
        raised.Should().Contain(nameof(row.ProfitIsNegative));
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
        raised.Should().Contain(nameof(row.PriceFetchFailed));
        raised.Should().Contain(nameof(row.IsLoadingPrice));
    }
}
