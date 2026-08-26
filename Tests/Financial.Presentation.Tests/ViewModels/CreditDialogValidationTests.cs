using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class CreditDialogValidationTests
{
    private static readonly DateTime ValidDate = new(2026, 7, 15);

    [Fact]
    public void BuildValidationMessage_DeleteMode_ReturnsEmpty()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: true, date: DateTime.MinValue, type: null, value: -1);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_AllFieldsValid_ReturnsEmpty()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_DateIsMinValue_IncludesDateError()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: DateTime.MinValue, type: "Dividend", value: 10);

        result.Should().Contain("Date is required.");
    }

    [Fact]
    public void BuildValidationMessage_InvalidType_IncludesTypeError()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Invalid", value: 10);

        result.Should().Contain("Type must be Dividend, Rent, or JCP.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildValidationMessage_ValueNotPositive_IncludesValueError(decimal value)
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: value);

        result.Should().Contain("Value must be greater than zero.");
    }

    [Fact]
    public void BuildValidationMessage_AllFieldsInvalid_IncludesEveryError()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: DateTime.MinValue, type: null, value: 0);

        result.Should().Contain("Date is required.");
        result.Should().Contain("Type must be Dividend, Rent, or JCP.");
        result.Should().Contain("Value must be greater than zero.");
    }

    [Theory]
    [InlineData("Dividend", true)]
    [InlineData("rent", true)]
    [InlineData("JCP", true)]
    [InlineData("Invalid", false)]
    [InlineData(null, false)]
    public void IsValidCreditType_ReturnsExpectedResult(string? value, bool expected)
    {
        CreditDialogValidation.IsValidCreditType(value).Should().Be(expected);
    }
}
