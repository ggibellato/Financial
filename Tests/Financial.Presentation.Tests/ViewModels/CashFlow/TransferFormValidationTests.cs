using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class TransferFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(
        DateTime? date, string sourceBank = "Barclays", string destinationBank = "Chase", string amount = "100") =>
        TransferFormValidation.BuildValidationMessage(date, sourceBank, destinationBank, amount);

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
        Validate(ValidDate, sourceBank: "").Should().Contain("Source bank is required.");
    }

    [Fact]
    public void MissingDestinationBank_ReturnsError()
    {
        Validate(ValidDate, destinationBank: "").Should().Contain("Destination bank is required.");
    }

    [Fact]
    public void SameSourceAndDestination_ReturnsError()
    {
        Validate(ValidDate, sourceBank: "Barclays", destinationBank: "Barclays")
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
