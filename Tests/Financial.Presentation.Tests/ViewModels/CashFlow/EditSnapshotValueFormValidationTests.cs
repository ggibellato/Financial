using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class EditSnapshotValueFormValidationTests
{
    [Fact]
    public void ValidValue_ReturnsEmpty()
    {
        EditSnapshotValueFormValidation.BuildValidationMessage("100").Should().BeEmpty();
    }

    [Fact]
    public void ZeroValue_IsAccepted()
    {
        EditSnapshotValueFormValidation.BuildValidationMessage("0").Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    public void InvalidOrNegativeValue_ReturnsError(string value)
    {
        EditSnapshotValueFormValidation.BuildValidationMessage(value).Should().Contain("Value must be a non-negative number.");
    }
}
