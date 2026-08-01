using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class EditReserveMovementFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(string bucket, string amount, DateTime? date, string description) =>
        EditReserveMovementFormValidation.BuildValidationMessage(bucket, amount, date, description);

    private static string Validate(string bucket = "Investimento", string amount = "50", string description = "Groceries") =>
        Validate(bucket, amount, ValidDate, description);

    [Fact]
    public void ValidForm_ReturnsEmpty()
    {
        Validate().Should().BeEmpty();
    }

    [Fact]
    public void NegativeAmount_IsAccepted()
    {
        Validate(amount: "-50").Should().BeEmpty();
    }

    [Fact]
    public void MissingBucket_ReturnsError()
    {
        Validate(bucket: "").Should().Contain("Bucket is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void NonNumericAmount_ReturnsError(string amount)
    {
        Validate(amount: amount).Should().Contain("Amount must be a number.");
    }

    [Fact]
    public void MissingDate_ReturnsError()
    {
        Validate("Investimento", "50", null, "Groceries").Should().Contain("Date is required.");
    }

    [Fact]
    public void MissingDescription_ReturnsError()
    {
        Validate(description: "").Should().Contain("Description is required.");
    }
}
