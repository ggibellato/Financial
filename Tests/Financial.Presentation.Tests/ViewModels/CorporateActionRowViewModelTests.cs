using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class CorporateActionRowViewModelTests
{
    private const string AffectedAssetName = "BBAS3";

    [Theory]
    [InlineData(CorporateAction.CorporateActionType.Split, "Split")]
    [InlineData(CorporateAction.CorporateActionType.Merger, "Merger")]
    [InlineData(CorporateAction.CorporateActionType.SpinOff, "Spin-off")]
    public void TypeLabel_MapsEachTypeToItsDisplayLabel(CorporateAction.CorporateActionType type, string expectedLabel)
    {
        var row = new CorporateActionRowViewModel(new CorporateActionDTO { Id = Guid.NewGuid(), Type = type, EffectiveDate = DateTime.Today }, AffectedAssetName);

        row.TypeLabel.Should().Be(expectedLabel);
    }

    [Fact]
    public void ResultingChange_Split_ShowsRescaledRatio()
    {
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Split, EffectiveDate = DateTime.Today, RatioFactor = 2m },
            AffectedAssetName);

        row.ResultingChange.Should().Be("Quantity/average cost rescaled ×2");
    }

    [Fact]
    public void ResultingChange_MergerSourceRole_ShowsPositionClosed()
    {
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Merger, EffectiveDate = DateTime.Today, Role = CorporateAction.CorporateActionRole.Source },
            AffectedAssetName);

        row.ResultingChange.Should().Be("Position closed and converted");
    }

    [Fact]
    public void ResultingChange_MergerTargetRoleWithConvertedQuantity_ShowsUnitsReceived()
    {
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO
            {
                Id = Guid.NewGuid(),
                Type = CorporateAction.CorporateActionType.Merger,
                EffectiveDate = DateTime.Today,
                Role = CorporateAction.CorporateActionRole.Target,
                ConvertedQuantity = 150m
            },
            AffectedAssetName);

        row.ResultingChange.Should().Be("+150.00 units received");
    }

    [Fact]
    public void ResultingChange_MergerTargetRoleWithoutConvertedQuantity_ShowsGenericMessage()
    {
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO
            {
                Id = Guid.NewGuid(),
                Type = CorporateAction.CorporateActionType.Merger,
                EffectiveDate = DateTime.Today,
                Role = CorporateAction.CorporateActionRole.Target
            },
            AffectedAssetName);

        row.ResultingChange.Should().Be("Units received from merger");
    }

    [Fact]
    public void ResultingChange_SpinOff_IsPlaceholderUntilPR4()
    {
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.SpinOff, EffectiveDate = DateTime.Today },
            AffectedAssetName);

        row.ResultingChange.Should().Be("—");
    }

    [Theory]
    [InlineData(CorporateAction.CorporateActionType.Split, true)]
    [InlineData(CorporateAction.CorporateActionType.Merger, true)]
    [InlineData(CorporateAction.CorporateActionType.SpinOff, false)]
    public void CanEditOrDelete_TrueForSplitAndMerger_FalseForSpinOff(CorporateAction.CorporateActionType type, bool expected)
    {
        var row = new CorporateActionRowViewModel(new CorporateActionDTO { Id = Guid.NewGuid(), Type = type, EffectiveDate = DateTime.Today }, AffectedAssetName);

        row.CanEditOrDelete.Should().Be(expected);
    }

    [Fact]
    public void HasCalculationStatus_NullStatus_IsFalse()
    {
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO { Id = Guid.NewGuid(), Type = CorporateAction.CorporateActionType.Merger, EffectiveDate = DateTime.Today, CalculationStatus = null },
            AffectedAssetName);

        row.HasCalculationStatus.Should().BeFalse();
    }

    [Fact]
    public void HasCalculationStatus_RequiresReview_ResolvesTheSharedStatusBadge()
    {
        var row = new CorporateActionRowViewModel(
            new CorporateActionDTO
            {
                Id = Guid.NewGuid(),
                Type = CorporateAction.CorporateActionType.Merger,
                EffectiveDate = DateTime.Today,
                CalculationStatus = CalculationStatus.RequiresReview
            },
            AffectedAssetName);

        row.HasCalculationStatus.Should().BeTrue();
        row.StatusLabel.Should().Be("Requires review");
        row.StatusAccessibleLabel.Should().Be("Requires review");
        row.StatusBrush.Should().NotBeNull();
        row.StatusForeground.Should().NotBeNull();
    }
}
