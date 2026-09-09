using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class PortfolioSummaryColumnWidthsTests
{
    [Fact]
    public void Constructor_SetsDefaultWidths()
    {
        var widths = new PortfolioSummaryColumnWidths();

        widths.AssetName.Should().Be(160);
        widths.Xirr.Should().Be(70);
        widths.EstAnnualPercent.Should().Be(70);
    }

    [Fact]
    public void AssetName_SetToDifferentValue_RaisesPropertyChanged()
    {
        var widths = new PortfolioSummaryColumnWidths();
        var raised = new List<string?>();
        widths.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        widths.AssetName = 200;

        widths.AssetName.Should().Be(200);
        raised.Should().ContainSingle().Which.Should().Be(nameof(PortfolioSummaryColumnWidths.AssetName));
    }

    [Fact]
    public void AssetName_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        var widths = new PortfolioSummaryColumnWidths();
        var raised = new List<string?>();
        widths.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        widths.AssetName = widths.AssetName;

        raised.Should().BeEmpty();
    }

    [Fact]
    public void RemainingColumnWidths_CanBeSetAndRead()
    {
        var widths = new PortfolioSummaryColumnWidths
        {
            FirstInvestment = 111,
            Quantity = 112,
            TotalInvested = 113,
            PortfolioWeight = 91,
            TotalCredits = 101,
            CurrentValue = 102,
            AveragePrice = 103,
            CurrentPrice = 104,
            Profit = 71,
            ProfitWithCredits = 114,
            LastMonthCredits = 105,
            LastCreditMonth = 92,
            LastMonthPercent = 72,
            EstAnnualCredits = 106,
        };

        widths.Should().BeEquivalentTo(new
        {
            FirstInvestment = 111,
            Quantity = 112,
            TotalInvested = 113,
            PortfolioWeight = 91,
            TotalCredits = 101,
            CurrentValue = 102,
            AveragePrice = 103,
            CurrentPrice = 104,
            Profit = 71,
            ProfitWithCredits = 114,
            LastMonthCredits = 105,
            LastCreditMonth = 92,
            LastMonthPercent = 72,
            EstAnnualCredits = 106,
        });
    }
}
