using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class TransactionDialogValidationTests
{
    private static readonly DateTime ValidDate = new(2026, 7, 15);

    [Fact]
    public void BuildValidationMessage_DeleteMode_ReturnsEmpty()
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: true, date: DateTime.MinValue, type: null, quantity: -1, unitPrice: -1, fees: -1, withheld: 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_AllFieldsValid_ReturnsEmpty()
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Buy", quantity: 10, unitPrice: 5, fees: 0, withheld: 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_DateIsMinValue_IncludesDateError()
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: DateTime.MinValue, type: "Buy", quantity: 10, unitPrice: 5, fees: 0, withheld: 0);

        result.Should().Contain("Date is required.");
    }

    [Fact]
    public void BuildValidationMessage_InvalidType_IncludesTypeError()
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Invalid", quantity: 10, unitPrice: 5, fees: 0, withheld: 0);

        result.Should().Contain("Type must be Buy, Sell, Fee, Redemption, Transfer In, Transfer Out, Capital Call, or Return of Capital.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildValidationMessage_QuantityNotPositive_IncludesQuantityError(decimal quantity)
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Buy", quantity: quantity, unitPrice: 5, fees: 0, withheld: 0);

        result.Should().Contain("Quantity must be greater than zero.");
    }

    [Fact]
    public void BuildValidationMessage_NegativeUnitPrice_IncludesUnitPriceError()
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Buy", quantity: 10, unitPrice: -1, fees: 0, withheld: 0);

        result.Should().Contain("Unit price cannot be negative.");
    }

    [Fact]
    public void BuildValidationMessage_NegativeFees_IncludesFeesError()
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Buy", quantity: 10, unitPrice: 5, fees: -1, withheld: 0);

        result.Should().Contain("Fees cannot be negative.");
    }

    [Fact]
    public void BuildValidationMessage_NegativeWithheld_IncludesWithheldError()
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Buy", quantity: 10, unitPrice: 5, fees: 0, withheld: -1);

        result.Should().Contain("Withheld cannot be negative.");
    }

    [Fact]
    public void BuildValidationMessage_TypeWithNoQuantityEffect_DoesNotRequireQuantityOrUnitPrice()
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, type: "Fee", quantity: 0, unitPrice: 0, fees: 5, withheld: 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_AllFieldsInvalid_IncludesEveryError()
    {
        var result = TransactionDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: DateTime.MinValue, type: null, quantity: 0, unitPrice: -1, fees: -1, withheld: -1);

        result.Should().Contain("Date is required.");
        result.Should().Contain("Type must be Buy, Sell, Fee, Redemption, Transfer In, Transfer Out, Capital Call, or Return of Capital.");
        result.Should().Contain("Quantity must be greater than zero.");
        result.Should().Contain("Unit price cannot be negative.");
        result.Should().Contain("Fees cannot be negative.");
        result.Should().Contain("Withheld cannot be negative.");
    }

    [Theory]
    [InlineData("Buy", true)]
    [InlineData("sell", true)]
    [InlineData("Invalid", false)]
    [InlineData(null, false)]
    public void IsValidTransactionType_ReturnsExpectedResult(string? value, bool expected)
    {
        TransactionDialogValidation.IsValidTransactionType(value).Should().Be(expected);
    }
}
