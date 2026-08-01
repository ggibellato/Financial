using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class WithdrawalFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(string bucket, string amount, DateTime? date, string description) =>
        WithdrawalFormValidation.BuildValidationMessage(bucket, amount, date, description);

    private static string Validate(string bucket = "Investimento", string amount = "50", string description = "Groceries") =>
        Validate(bucket, amount, ValidDate, description);

    [Fact]
    public void ValidForm_ReturnsEmpty()
    {
        Validate().Should().BeEmpty();
    }

    [Fact]
    public void MissingBucket_ReturnsError()
    {
        Validate(bucket: "").Should().Contain("Bucket is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    public void InvalidOrNonPositiveAmount_ReturnsError(string amount)
    {
        Validate(amount: amount).Should().Contain("Amount must be a positive number.");
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
