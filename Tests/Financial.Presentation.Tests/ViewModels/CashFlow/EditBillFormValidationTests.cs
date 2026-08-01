using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class EditBillFormValidationTests
{
    private static string Validate(string value = "100", string status = "Paid") =>
        EditBillFormValidation.BuildValidationMessage(value, status);

    [Fact]
    public void ValidForm_ReturnsEmpty()
    {
        Validate().Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void InvalidValue_ReturnsError(string value)
    {
        Validate(value: value).Should().Contain("Value must be a number.");
    }

    [Fact]
    public void MissingStatus_ReturnsError()
    {
        Validate(status: "").Should().Contain("Status is required.");
    }
}
