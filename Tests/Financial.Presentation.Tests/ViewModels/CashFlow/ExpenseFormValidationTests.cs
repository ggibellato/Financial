using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class ExpenseFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(
        DateTime? date,
        string description = "Groceries",
        string category = "Mercado",
        string value = "10",
        bool isCardMode = false,
        Guid? paymentSource = null,
        Guid? creditCardId = null,
        bool showRoundUpField = false,
        string roundUpAmount = "") =>
        ExpenseFormValidation.BuildValidationMessage(
            date, description, category, value, isCardMode, paymentSource ?? Guid.NewGuid(), creditCardId, showRoundUpField, roundUpAmount);

    [Fact]
    public void ValidBankModeForm_ReturnsEmpty()
    {
        Validate(ValidDate).Should().BeEmpty();
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

    [Fact]
    public void MissingCategory_ReturnsError()
    {
        Validate(ValidDate, category: "").Should().Contain("Category is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    public void InvalidOrZeroValue_ReturnsError(string value)
    {
        Validate(ValidDate, value: value).Should().Contain("Value must be a non-zero number.");
    }

    [Fact]
    public void BankMode_MissingPaymentSource_ReturnsError()
    {
        ExpenseFormValidation.BuildValidationMessage(
            ValidDate, "Groceries", "Mercado", "10", isCardMode: false, paymentSource: null, null, false, "")
            .Should().Contain("Payment Source is required.");
    }

    [Fact]
    public void CardMode_MissingCreditCardId_ReturnsError()
    {
        Validate(ValidDate, isCardMode: true, creditCardId: null).Should().Contain("Card is required when charging to a card.");
    }

    [Fact]
    public void CardMode_WithCreditCardId_DoesNotRequirePaymentSource()
    {
        Validate(ValidDate, isCardMode: true, creditCardId: Guid.NewGuid(), paymentSource: null).Should().BeEmpty();
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.00")]
    public void RoundUpOutOfRange_ReturnsError(string roundUp)
    {
        Validate(ValidDate, showRoundUpField: true, roundUpAmount: roundUp)
            .Should().Contain("Round-up amount must be between £0.00 and £0.99.");
    }

    [Theory]
    [InlineData("0.00")]
    [InlineData("0.99")]
    public void RoundUpAtBoundary_IsAccepted(string roundUp)
    {
        Validate(ValidDate, showRoundUpField: true, roundUpAmount: roundUp).Should().BeEmpty();
    }

    [Fact]
    public void RoundUpField_NotShown_IgnoresValue()
    {
        Validate(ValidDate, showRoundUpField: false, roundUpAmount: "99.99").Should().BeEmpty();
    }
}
