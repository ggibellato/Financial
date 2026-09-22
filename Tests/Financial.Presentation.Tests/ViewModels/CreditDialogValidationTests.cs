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
            isDeleteMode: true, date: DateTime.MinValue, type: null, value: -1, withheld: 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_AllFieldsValid_ReturnsEmpty()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: 10, withheld: 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_DateIsMinValue_IncludesDateError()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: DateTime.MinValue, type: "Dividend", value: 10, withheld: 0);

        result.Should().Contain("Date is required.");
    }

    [Fact]
    public void BuildValidationMessage_InvalidType_IncludesTypeError()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Invalid", value: 10, withheld: 0);

        result.Should().Contain("Type must be Dividend, Securities Lending Income, JCP, or Coupon.");
    }

    [Fact]
    public void BuildValidationMessage_ValueZero_IncludesValueError()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: 0, withheld: 0);

        result.Should().Contain("Value must not be zero.");
    }

    [Fact]
    public void BuildValidationMessage_NegativeValueCorrection_ReturnsEmpty()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: -10, withheld: 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_WithheldOppositeSignToValue_IncludesWithheldError()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: 10, withheld: -1);

        result.Should().Contain("Withheld must share Value's sign and must not exceed it in magnitude.");
    }

    [Fact]
    public void BuildValidationMessage_WithheldExceedsValueMagnitude_IncludesWithheldError()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: 10, withheld: 11);

        result.Should().Contain("Withheld must share Value's sign and must not exceed it in magnitude.");
    }

    [Fact]
    public void BuildValidationMessage_AllFieldsInvalid_IncludesEveryError()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: DateTime.MinValue, type: null, value: 0, withheld: 1);

        result.Should().Contain("Date is required.");
        result.Should().Contain("Type must be Dividend, Securities Lending Income, JCP, or Coupon.");
        result.Should().Contain("Value must not be zero.");
        result.Should().Contain("Withheld must share Value's sign and must not exceed it in magnitude.");
    }

    [Fact]
    public void BuildValidationMessage_NullSharesForDividend_ReturnsEmpty()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: 10, withheld: 0, sharesForDividend: null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_PositiveSharesForDividend_ReturnsEmpty()
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: 10, withheld: 0, sharesForDividend: 800m);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildValidationMessage_NonPositiveSharesForDividend_IncludesSharesError(decimal sharesForDividend)
    {
        var result = CreditDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Dividend", value: 10, withheld: 0, sharesForDividend: sharesForDividend);

        result.Should().Contain("Shares for this dividend must be greater than zero when provided.");
    }

    [Theory]
    [InlineData("Dividend", true)]
    [InlineData("securitieslendingincome", true)]
    [InlineData("JCP", true)]
    [InlineData("Coupon", true)]
    [InlineData("Invalid", false)]
    [InlineData(null, false)]
    public void IsValidCreditType_ReturnsExpectedResult(string? value, bool expected)
    {
        CreditDialogValidation.IsValidCreditType(value).Should().Be(expected);
    }
}
