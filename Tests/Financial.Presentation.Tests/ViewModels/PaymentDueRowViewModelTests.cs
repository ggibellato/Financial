using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;
using Wpf.Ui.Controls;

namespace Financial.Presentation.Tests.ViewModels;

public class PaymentDueRowViewModelTests
{
    private static PaymentDueDTO Payment(string type = "Mensais", string name = "Internet", int daysRemaining = 0) => new()
    {
        Type = type,
        Name = name,
        DueDate = new DateOnly(2026, 9, 5),
        DaysRemaining = daysRemaining,
    };

    [Fact]
    public void Constructor_WithNullPayment_Throws()
    {
        Action act = () => new PaymentDueRowViewModel(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("payment");
    }

    [Fact]
    public void DaysRemainingZero_MapsToTodayTier()
    {
        var row = new PaymentDueRowViewModel(Payment(daysRemaining: 0));

        row.UrgencySymbol.Should().Be(SymbolRegular.AlertUrgent20);
        row.UrgencySymbolFilled.Should().BeTrue();
        row.DaysRemainingText.Should().Be("Due today");
        row.UrgencyAccessibleLabel.Should().Contain("urgent");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void DaysRemainingOneOrTwo_MapsToSoonTier(int daysRemaining)
    {
        var row = new PaymentDueRowViewModel(Payment(daysRemaining: daysRemaining));

        row.UrgencySymbol.Should().Be(SymbolRegular.Clock20);
        row.UrgencySymbolFilled.Should().BeFalse();
        row.UrgencyAccessibleLabel.Should().Contain("soon");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void DaysRemainingThreeToFive_MapsToUpcomingTier(int daysRemaining)
    {
        var row = new PaymentDueRowViewModel(Payment(daysRemaining: daysRemaining));

        row.UrgencySymbol.Should().Be(SymbolRegular.Calendar20);
        row.UrgencySymbolFilled.Should().BeFalse();
        row.UrgencyAccessibleLabel.Should().Contain("upcoming");
    }

    [Fact]
    public void DaysRemainingText_OneDayIsSingular()
    {
        var row = new PaymentDueRowViewModel(Payment(daysRemaining: 1));

        row.DaysRemainingText.Should().Be("Due in 1 day");
    }

    [Fact]
    public void DaysRemainingText_MultipleDaysIsPlural()
    {
        var row = new PaymentDueRowViewModel(Payment(daysRemaining: 3));

        row.DaysRemainingText.Should().Be("Due in 3 days");
    }

    [Fact]
    public void TypeLabel_CreditCard_DisplaysAsCreditCardWithSpace()
    {
        var row = new PaymentDueRowViewModel(Payment(type: "CreditCard"));

        row.TypeLabel.Should().Be("Credit card");
    }

    [Fact]
    public void TypeLabel_Mensais_DisplaysAsIs()
    {
        var row = new PaymentDueRowViewModel(Payment(type: "Mensais"));

        row.TypeLabel.Should().Be("Mensais");
    }

    [Fact]
    public void NameAndDueDate_AreCopiedFromThePayment()
    {
        var row = new PaymentDueRowViewModel(Payment(name: "Nubank"));

        row.Name.Should().Be("Nubank");
        row.DueDate.Should().Be(new DateOnly(2026, 9, 5));
    }
}
