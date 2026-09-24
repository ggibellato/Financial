using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class CorporateActionFormValidationTests
{
    private static readonly DateTime ValidDate = new(2026, 7, 15);

    [Fact]
    public void BuildValidationMessage_DeleteMode_ReturnsEmpty()
    {
        var result = CorporateActionFormValidation.BuildValidationMessage(
            isDeleteMode: true, effectiveDate: DateTime.MinValue, ratioNumerator: -1, ratioDenominator: -1);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_AllFieldsValid_ReturnsEmpty()
    {
        var result = CorporateActionFormValidation.BuildValidationMessage(
            isDeleteMode: false, effectiveDate: ValidDate, ratioNumerator: 3, ratioDenominator: 1);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_EffectiveDateIsMinValue_IncludesDateError()
    {
        var result = CorporateActionFormValidation.BuildValidationMessage(
            isDeleteMode: false, effectiveDate: DateTime.MinValue, ratioNumerator: 3, ratioDenominator: 1);

        result.Should().Contain("Effective date is required");
    }

    [Fact]
    public void BuildValidationMessage_RatioIsOneForOne_IncludesRatioError()
    {
        var result = CorporateActionFormValidation.BuildValidationMessage(
            isDeleteMode: false, effectiveDate: ValidDate, ratioNumerator: 1, ratioDenominator: 1);

        result.Should().Contain("Enter a valid split ratio other than 1-for-1");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(3, 0)]
    [InlineData(3, -1)]
    public void BuildValidationMessage_NonPositiveRatioComponent_IncludesRatioError(decimal numerator, decimal denominator)
    {
        var result = CorporateActionFormValidation.BuildValidationMessage(
            isDeleteMode: false, effectiveDate: ValidDate, ratioNumerator: numerator, ratioDenominator: denominator);

        result.Should().Contain("Enter a valid split ratio other than 1-for-1");
    }

    [Theory]
    [InlineData(3, 1, true, 3)]
    [InlineData(1, 1, false, 1)]
    [InlineData(0, 1, false, 0)]
    [InlineData(1, 0, false, 0)]
    [InlineData(1, 10, true, 0.1)]
    public void TryComputeSplitRatioFactor_ComputesExpectedFactor(decimal numerator, decimal denominator, bool expectedResult, decimal expectedFactor)
    {
        var result = CorporateActionFormValidation.TryComputeSplitRatioFactor(numerator, denominator, out var factor);

        result.Should().Be(expectedResult);
        factor.Should().Be(expectedFactor);
    }
}
