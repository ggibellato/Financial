using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class IncomeSplitFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(DateTime? date, string amount = "100", string description = "Salary") =>
        IncomeSplitFormValidation.BuildValidationMessage(date, amount, description);

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
    [InlineData("0")]
    [InlineData("-5")]
    public void InvalidOrNonPositiveAmount_ReturnsError(string amount)
    {
        Validate(ValidDate, amount: amount).Should().Contain("Amount must be a positive number.");
    }

    [Fact]
    public void MissingDescription_ReturnsError()
    {
        Validate(ValidDate, description: "").Should().Contain("Description is required.");
    }
}
