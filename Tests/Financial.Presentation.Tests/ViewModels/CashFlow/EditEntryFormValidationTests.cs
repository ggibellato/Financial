using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class EditEntryFormValidationTests
{
    private static string Validate(string brlValue = "100", string gbpValue = "20") =>
        EditEntryFormValidation.BuildValidationMessage(brlValue, gbpValue);

    [Fact]
    public void ValidForm_ReturnsEmpty()
    {
        Validate().Should().BeEmpty();
    }

    [Fact]
    public void BothBlank_IsAccepted()
    {
        Validate(brlValue: "", gbpValue: "").Should().BeEmpty();
    }

    [Fact]
    public void NegativeValues_AreAccepted()
    {
        Validate(brlValue: "-100", gbpValue: "-20").Should().BeEmpty();
    }

    [Fact]
    public void NonNumericBrl_ReturnsError()
    {
        Validate(brlValue: "abc").Should().Contain("BRL value must be a number.");
    }

    [Fact]
    public void NonNumericGbp_ReturnsError()
    {
        Validate(gbpValue: "abc").Should().Contain("GBP value must be a number.");
    }
}
