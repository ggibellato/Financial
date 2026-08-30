using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class AssetFormDialogViewModelTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> PortfoliosByBroker =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["XPI"] = ["Default", "ISA"],
            ["Avenue"] = ["Growth"],
        };

    [Fact]
    public void Constructor_NoExisting_IsCreateModeWithFirstBrokerAndItsPortfoliosSelected()
    {
        var viewModel = new AssetFormDialogViewModel(PortfoliosByBroker);

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Asset");
        viewModel.Name.Should().BeEmpty();
        viewModel.BrokerName.Should().Be("XPI");
        viewModel.PortfolioName.Should().Be("Default");
        viewModel.PortfolioNames.Should().Equal("Default", "ISA");
        viewModel.CanChangeBrokerPortfolio.Should().BeTrue();
    }

    [Fact]
    public void BrokerName_Changed_RescopesPortfolioNamesAndResetsSelection()
    {
        var viewModel = new AssetFormDialogViewModel(PortfoliosByBroker);

        viewModel.BrokerName = "Avenue";

        viewModel.PortfolioNames.Should().Equal("Growth");
        viewModel.PortfolioName.Should().Be("Growth");
    }

    [Fact]
    public void Constructor_WithExisting_IsEditModePreFilledWithFixedBrokerPortfolioAndIdentity()
    {
        var existing = new AssetAdminDTO
        {
            Name = "BCIA11",
            BrokerName = "XPI",
            PortfolioName = "Default",
            BrokerStatus = "Active",
            ISIN = "BR0000000001",
            Exchange = "BVMF",
            Ticker = "BCIA11",
            Country = CountryCode.BR,
            LocalTypeCode = "FII",
            Class = GlobalAssetClass.RealEstate,
            Quantity = 100,
        };

        var viewModel = new AssetFormDialogViewModel(new Dictionary<string, IReadOnlyList<string>>(), existing);

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Asset");
        viewModel.BrokerName.Should().Be("XPI");
        viewModel.PortfolioName.Should().Be("Default");
        viewModel.CanChangeBrokerPortfolio.Should().BeFalse();
        viewModel.Name.Should().Be("BCIA11");
        viewModel.ISIN.Should().Be("BR0000000001");
        viewModel.Country.Should().Be(CountryCode.BR);
        viewModel.Class.Should().Be(GlobalAssetClass.RealEstate);
    }

    [Fact]
    public void Constructor_BlankName_StartsInvalid()
    {
        var viewModel = new AssetFormDialogViewModel(PortfoliosByBroker) { Name = string.Empty };

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Name_SetToNonBlank_BecomesValid()
    {
        var viewModel = new AssetFormDialogViewModel(PortfoliosByBroker) { Name = "NEWASSET" };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ISIN_InvalidFormat_IsInvalid()
    {
        var viewModel = new AssetFormDialogViewModel(PortfoliosByBroker) { Name = "NEWASSET", ISIN = "NOT-AN-ISIN" };

        viewModel.ValidationMessage.Should().Contain("ISIN");
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ISIN_Blank_IsValid()
    {
        var viewModel = new AssetFormDialogViewModel(PortfoliosByBroker) { Name = "NEWASSET", ISIN = "" };

        viewModel.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmCommand_ValidName_TrimsNameAndIsinAndRaisesCloseRequestedTrue()
    {
        var viewModel = new AssetFormDialogViewModel(PortfoliosByBroker) { Name = "  NEWASSET  ", ISIN = " US0378331005 " };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Name.Should().Be("NEWASSET");
        viewModel.ISIN.Should().Be("US0378331005");
    }

    [Fact]
    public void ConfirmCommand_BlankName_DoesNotRaiseCloseRequested()
    {
        var viewModel = new AssetFormDialogViewModel(PortfoliosByBroker) { Name = string.Empty };
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new AssetFormDialogViewModel(PortfoliosByBroker);
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }
}
