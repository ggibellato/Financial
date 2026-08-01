using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class CreateEntryFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(DateTime? date, string description = "Salary", string value = "100") =>
        CreateEntryFormValidation.BuildValidationMessage(date, description, value);

    [Fact]
    public void ValidForm_ReturnsEmpty()
    {
        Validate(ValidDate).Should().BeEmpty();
    }

    [Fact]
    public void NegativeValue_IsAccepted()
    {
        Validate(ValidDate, value: "-50").Should().BeEmpty();
    }

    [Fact]
    public void MissingDate_ReturnsError()
    {
        Validate(date: null).Should().Contain("Date is required.");
    }

    [Fact]
    public void MissingDescription_ReturnsError()
    {
        Validate(ValidDate, description: "").Should().Contain("Description is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    public void InvalidOrZeroValue_ReturnsError(string value)
    {
        Validate(ValidDate, value: value).Should().Contain("Value must be a non-zero number.");
    }
}
