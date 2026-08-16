using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class IncomeFormValidationTests
{
    private static readonly DateTime ValidDate = DateTime.Today;

    private static string Validate(
        DateTime? date,
        Guid? incomeSource = null,
        string netValue = "100") =>
        IncomeFormValidation.BuildValidationMessage(date, incomeSource ?? Guid.NewGuid(), netValue);

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
        IncomeFormValidation.BuildValidationMessage(ValidDate, null, "100")
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
    public void MissingBank_DoesNotReturnError()
    {
        IncomeFormValidation.BuildValidationMessage(ValidDate, Guid.NewGuid(), "100")
            .Should().BeEmpty();
    }
}
