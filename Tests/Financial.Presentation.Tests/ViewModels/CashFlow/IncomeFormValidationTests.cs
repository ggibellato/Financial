using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class IncomeFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(
        DateTime? date,
        string incomeSource = "Gleison",
        string netValue = "100",
        string bank = "Barclays") =>
        IncomeFormValidation.BuildValidationMessage(date, incomeSource, netValue, bank);

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
    public void MissingSource_ReturnsError()
    {
        Validate(ValidDate, incomeSource: "").Should().Contain("Source is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void InvalidNetValue_ReturnsError(string netValue)
    {
        Validate(ValidDate, netValue: netValue).Should().Contain("Net Value must be a number.");
    }

    [Fact]
    public void MissingBank_ReturnsError()
    {
        Validate(ValidDate, bank: "").Should().Contain("Bank is required.");
    }
}
