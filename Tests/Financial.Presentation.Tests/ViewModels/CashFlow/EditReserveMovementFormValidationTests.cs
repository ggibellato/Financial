using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class EditReserveMovementFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;
    private static readonly Guid ValidBucketId = Guid.NewGuid();

    private static string Validate(Guid? bucketId, string amount, DateTime? date, string description) =>
        EditReserveMovementFormValidation.BuildValidationMessage(bucketId, amount, date, description);

    private static string Validate(Guid? bucketId = null, string amount = "50", string description = "Groceries") =>
        Validate(bucketId ?? ValidBucketId, amount, ValidDate, description);

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

    private static string ValidateWithoutBucket() =>
        Validate(null, "50", ValidDate, "Groceries");

    [Fact]
    public void MissingBucket_ReturnsError()
    {
        ValidateWithoutBucket().Should().Contain("Bucket is required.");
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
        Validate(ValidBucketId, "50", null, "Groceries").Should().Contain("Date is required.");
    }

    [Fact]
    public void MissingDescription_ReturnsError()
    {
        Validate(description: "").Should().Contain("Description is required.");
    }
}
