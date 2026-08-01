using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class AddBillFormValidationTests
{
    private static string Validate(string description = "Rent", string dueDay = "10", string value = "100") =>
        AddBillFormValidation.BuildValidationMessage(description, dueDay, value);

    [Fact]
    public void ValidForm_ReturnsEmpty()
    {
        Validate().Should().BeEmpty();
    }

    [Fact]
    public void MissingDescription_ReturnsError()
    {
        Validate(description: "").Should().Contain("Description is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("32")]
    [InlineData("1.5")]
    public void InvalidDueDay_ReturnsError(string dueDay)
    {
        Validate(dueDay: dueDay).Should().Contain("Due Day must be a whole number between 1 and 31.");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("31")]
    public void BoundaryDueDay_IsAccepted(string dueDay)
    {
        Validate(dueDay: dueDay).Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void InvalidValue_ReturnsError(string value)
    {
        Validate(value: value).Should().Contain("Value must be a number.");
    }
}
