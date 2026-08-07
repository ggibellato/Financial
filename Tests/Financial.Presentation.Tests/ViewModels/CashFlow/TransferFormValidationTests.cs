using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class TransferFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(
        DateTime? date, Guid? sourceBank = null, Guid? destinationBank = null, string amount = "100") =>
        TransferFormValidation.BuildValidationMessage(date, sourceBank ?? Guid.NewGuid(), destinationBank ?? Guid.NewGuid(), amount);

    [Fact]
    public void ValidForm_ReturnsEmpty()
    {
        Validate(ValidDate).Should().BeEmpty();
    }

    [Fact]
    public void MissingDate_ReturnsError()
    {
        Validate(date: null).Should().Contain("Date is required.");
    }

    [Fact]
    public void MissingSourceBank_ReturnsError()
    {
        TransferFormValidation.BuildValidationMessage(ValidDate, null, Guid.NewGuid(), "100")
            .Should().Contain("Source bank is required.");
    }

    [Fact]
    public void MissingDestinationBank_ReturnsError()
    {
        TransferFormValidation.BuildValidationMessage(ValidDate, Guid.NewGuid(), null, "100")
            .Should().Contain("Destination bank is required.");
    }

    [Fact]
    public void SameSourceAndDestination_ReturnsError()
    {
        var bankId = Guid.NewGuid();
        Validate(ValidDate, sourceBank: bankId, destinationBank: bankId)
            .Should().Contain("Source and destination must be different banks.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    public void InvalidOrNonPositiveAmount_ReturnsError(string amount)
    {
        Validate(ValidDate, amount: amount).Should().Contain("Amount must be a positive number.");
    }
}
