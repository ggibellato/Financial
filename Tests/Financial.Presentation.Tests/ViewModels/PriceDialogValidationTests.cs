using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

[Trait("Category", "Unit")]
public class PriceDialogValidationTests
{
    private static readonly DateTime ValidDate = new(2026, 7, 15);

    [Fact]
    public void BuildValidationMessage_DeleteMode_ReturnsEmpty()
    {
        var result = PriceDialogValidation.BuildValidationMessage(
            isDeleteMode: true, date: DateTime.MinValue, price: -1, TestClock.LocalToday);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_AllFieldsValid_ReturnsEmpty()
    {
        var result = PriceDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, price: 10m, TestClock.LocalToday);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_DateIsMinValue_IncludesDateError()
    {
        var result = PriceDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: DateTime.MinValue, price: 10m, TestClock.LocalToday);

        result.Should().Contain("Date is required.");
    }

    [Fact]
    public void BuildValidationMessage_FutureDate_IncludesFutureDateError()
    {
        var result = PriceDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: TestClock.LocalToday.AddDays(1), price: 10m, TestClock.LocalToday);

        result.Should().Contain("Price date cannot be in the future.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildValidationMessage_PriceNotPositive_IncludesPriceError(decimal price)
    {
        var result = PriceDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: ValidDate, price: price, TestClock.LocalToday);

        result.Should().Contain("Price must be greater than zero.");
    }

    [Fact]
    public void BuildValidationMessage_AllFieldsInvalid_IncludesEveryError()
    {
        var result = PriceDialogValidation.BuildValidationMessage(
            isDeleteMode: false, date: DateTime.MinValue, price: 0m, TestClock.LocalToday);

        result.Should().Contain("Date is required.");
        result.Should().Contain("Price must be greater than zero.");
    }
}
