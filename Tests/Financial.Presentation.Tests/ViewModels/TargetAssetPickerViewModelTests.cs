using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class TargetAssetPickerViewModelTests
{
    private static AssetAdminDTO Asset(string name) => new()
    {
        Name = name,
        BrokerName = "XPI",
        PortfolioName = "Default",
        BrokerStatus = "Active",
    };

    private static readonly IReadOnlyList<AssetAdminDTO> Candidates =
        [Asset("BBAS3"), Asset("PETR4"), Asset("ITUB4")];

    [Fact]
    public void CandidateNames_ReflectsConstructorCandidates()
    {
        var vm = new TargetAssetPickerViewModel(Candidates);

        vm.CandidateNames.Should().Equal("BBAS3", "ITUB4", "PETR4");
    }

    [Fact]
    public void AssetName_ExactMatch_ShowCreateFieldsFalseAndMatchedAssetSet()
    {
        var vm = new TargetAssetPickerViewModel(Candidates) { AssetName = "PETR4" };

        vm.MatchedAsset.Should().NotBeNull();
        vm.MatchedAsset!.Name.Should().Be("PETR4");
        vm.ShowCreateFields.Should().BeFalse();
    }

    [Fact]
    public void AssetName_NoMatchNonEmpty_ShowCreateFieldsTrue()
    {
        var vm = new TargetAssetPickerViewModel(Candidates) { AssetName = "NEWCO" };

        vm.MatchedAsset.Should().BeNull();
        vm.ShowCreateFields.Should().BeTrue();
    }

    [Fact]
    public void AssetName_Empty_ShowCreateFieldsFalseEvenWithNoMatch()
    {
        var vm = new TargetAssetPickerViewModel(Candidates) { AssetName = string.Empty };

        vm.MatchedAsset.Should().BeNull();
        vm.ShowCreateFields.Should().BeFalse();
    }

    [Fact]
    public void TrimmedAssetName_StripsLeadingAndTrailingWhitespace()
    {
        var vm = new TargetAssetPickerViewModel(Candidates) { AssetName = "  NEWCO  " };

        vm.TrimmedAssetName.Should().Be("NEWCO");
        vm.ShowCreateFields.Should().BeTrue();
    }

    [Fact]
    public void AssetName_MatchIsCaseInsensitiveAndTrims()
    {
        var vm = new TargetAssetPickerViewModel(Candidates) { AssetName = "  petr4  " };

        vm.MatchedAsset.Should().NotBeNull();
        vm.MatchedAsset!.Name.Should().Be("PETR4");
    }

    [Fact]
    public void IsinValidationMessage_Blank_IsNull()
    {
        var vm = new TargetAssetPickerViewModel(Candidates) { ISIN = string.Empty };

        vm.IsinValidationMessage.Should().BeNull();
    }

    [Fact]
    public void IsinValidationMessage_Valid_IsNull()
    {
        var vm = new TargetAssetPickerViewModel(Candidates) { ISIN = "US0378331005" };

        vm.IsinValidationMessage.Should().BeNull();
    }

    [Fact]
    public void IsinValidationMessage_Invalid_HasExactMessage()
    {
        var vm = new TargetAssetPickerViewModel(Candidates) { ISIN = "NOT-AN-ISIN" };

        vm.IsinValidationMessage.Should()
            .Be("ISIN must be 2 letters, 9 alphanumeric characters, and a check digit (e.g. US0378331005).");
    }

    [Fact]
    public void AssetName_Changed_ClearsPreviouslySetNameError()
    {
        var vm = new TargetAssetPickerViewModel(Candidates) { NameError = "Already exists." };

        vm.AssetName = "SOMETHINGELSE";

        vm.NameError.Should().BeNull();
    }
}
