using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class CorporateActionFormValidationTests
{
    private static readonly DateTime ValidDate = new(2026, 7, 15);
    private const string SplitType = CorporateActionFormValidation.SplitTypeValue;
    private const string MergerType = CorporateActionFormValidation.MergerTypeValue;
    private const string SpinOffType = CorporateActionFormValidation.SpinOffTypeValue;

    private static string Build(
        bool isDeleteMode = false,
        string type = SplitType,
        bool isAddMode = true,
        DateTime? effectiveDate = null,
        decimal ratioNumerator = 3m,
        decimal ratioDenominator = 1m,
        string targetAssetName = "Company B",
        decimal exchangeRatio = 2m,
        decimal quantityReceived = 10m,
        decimal allocationPercentage = 25m) =>
        CorporateActionFormValidation.BuildValidationMessage(
            isDeleteMode, type, isAddMode, effectiveDate ?? ValidDate, ratioNumerator, ratioDenominator, targetAssetName, exchangeRatio,
            quantityReceived, allocationPercentage);

    [Fact]
    public void BuildValidationMessage_DeleteMode_ReturnsEmpty()
    {
        var result = Build(isDeleteMode: true, effectiveDate: DateTime.MinValue, ratioNumerator: -1, ratioDenominator: -1);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_Split_AllFieldsValid_ReturnsEmpty()
    {
        var result = Build(type: SplitType, ratioNumerator: 3, ratioDenominator: 1);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_EffectiveDateIsMinValue_IncludesDateError()
    {
        var result = Build(effectiveDate: DateTime.MinValue);

        result.Should().Contain("Effective date is required");
    }

    [Fact]
    public void BuildValidationMessage_Split_RatioIsOneForOne_IncludesRatioError()
    {
        var result = Build(type: SplitType, ratioNumerator: 1, ratioDenominator: 1);

        result.Should().Contain("Enter a valid split ratio other than 1-for-1");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(3, 0)]
    [InlineData(3, -1)]
    public void BuildValidationMessage_Split_NonPositiveRatioComponent_IncludesRatioError(decimal numerator, decimal denominator)
    {
        var result = Build(type: SplitType, ratioNumerator: numerator, ratioDenominator: denominator);

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

    [Fact]
    public void BuildValidationMessage_Merger_AllFieldsValid_ReturnsEmpty()
    {
        var result = Build(type: MergerType, isAddMode: true, targetAssetName: "Company B", exchangeRatio: 2m);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_Merger_AddModeMissingTargetAsset_IncludesTargetAssetError()
    {
        var result = Build(type: MergerType, isAddMode: true, targetAssetName: "", exchangeRatio: 2m);

        result.Should().Contain("Target asset is required");
    }

    [Fact]
    public void BuildValidationMessage_Merger_UpdateModeMissingTargetAsset_DoesNotRequireTargetAsset()
    {
        var result = Build(type: MergerType, isAddMode: false, targetAssetName: "", exchangeRatio: 2m);

        result.Should().NotContain("Target asset is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildValidationMessage_Merger_ExchangeRatioNotPositive_IncludesExchangeRatioError(decimal exchangeRatio)
    {
        var result = Build(type: MergerType, exchangeRatio: exchangeRatio);

        result.Should().Contain("Exchange ratio must be greater than zero");
    }

    [Fact]
    public void BuildValidationMessage_Merger_EffectiveDateStillRequired()
    {
        var result = Build(type: MergerType, effectiveDate: DateTime.MinValue, exchangeRatio: 2m);

        result.Should().Contain("Effective date is required");
    }

    [Fact]
    public void BuildValidationMessage_SpinOff_AllFieldsValid_ReturnsEmpty()
    {
        var result = Build(type: SpinOffType, isAddMode: true, targetAssetName: "Company B", quantityReceived: 10m, allocationPercentage: 25m);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildValidationMessage_SpinOff_AddModeMissingNewAsset_IncludesNewAssetError()
    {
        var result = Build(type: SpinOffType, isAddMode: true, targetAssetName: "");

        result.Should().Contain("New asset is required");
    }

    [Fact]
    public void BuildValidationMessage_SpinOff_UpdateModeMissingNewAsset_DoesNotRequireNewAsset()
    {
        var result = Build(type: SpinOffType, isAddMode: false, targetAssetName: "");

        result.Should().NotContain("New asset is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildValidationMessage_SpinOff_QuantityReceivedNotPositive_IncludesQuantityError(decimal quantityReceived)
    {
        var result = Build(type: SpinOffType, quantityReceived: quantityReceived);

        result.Should().Contain("Quantity received must be greater than zero");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void BuildValidationMessage_SpinOff_AllocationPercentageOutOfRange_IncludesAllocationError(decimal allocationPercentage)
    {
        var result = Build(type: SpinOffType, allocationPercentage: allocationPercentage);

        result.Should().Contain("Allocation percentage must be between 0 and 100");
    }

    [Fact]
    public void BuildValidationMessage_SpinOff_EffectiveDateStillRequired()
    {
        var result = Build(type: SpinOffType, effectiveDate: DateTime.MinValue);

        result.Should().Contain("Effective date is required");
    }
}
