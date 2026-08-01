using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class BalanceAdjustmentFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(DateTime? date, string targetBalance = "100") =>
        BalanceAdjustmentFormValidation.BuildValidationMessage(date, targetBalance);

    [Fact]
    public void ValidForm_ReturnsEmpty()
    {
        Validate(ValidDate).Should().BeEmpty();
    }

    [Fact]
    public void MissingDate_ReturnsError()
    {
        Validate(date: null).Should().Contain("Date is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-0.01")]
    public void InvalidOrNegativeTargetBalance_ReturnsError(string targetBalance)
    {
        Validate(ValidDate, targetBalance: targetBalance).Should().Contain("Target Balance must be zero or greater.");
    }

    [Fact]
    public void ZeroTargetBalance_IsAccepted()
    {
        Validate(ValidDate, targetBalance: "0").Should().BeEmpty();
    }
}
