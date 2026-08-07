using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class IncomeFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(
        DateTime? date,
        Guid? incomeSource = null,
        string netValue = "100",
        Guid? bank = null) =>
        IncomeFormValidation.BuildValidationMessage(date, incomeSource ?? Guid.NewGuid(), netValue, bank ?? Guid.NewGuid());

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
        IncomeFormValidation.BuildValidationMessage(ValidDate, null, "100", Guid.NewGuid())
            .Should().Contain("Source is required.");
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
        IncomeFormValidation.BuildValidationMessage(ValidDate, Guid.NewGuid(), "100", null)
            .Should().Contain("Bank is required.");
    }
}
