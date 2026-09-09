using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class AssetsViewModelTests
{
    private static (AssetsViewModel ViewModel, StubAssetAdminService AssetAdminService, StubAssetMoveService AssetMoveService, StubPortfolioService PortfolioService, StubDialogService Dialog) CreateViewModel()
    {
        var assetAdminService = new StubAssetAdminService();
        var assetMoveService = new StubAssetMoveService();
        var portfolioService = new StubPortfolioService();
        var dialog = new StubDialogService();
        var viewModel = new AssetsViewModel(assetAdminService, assetMoveService, portfolioService, dialog, new RecordingLogger<AssetsViewModel>());
        return (viewModel, assetAdminService, assetMoveService, portfolioService, dialog);
    }

    private static AssetAdminDTO MakeAsset(string name, string broker, string portfolio, decimal quantity) => new()
    {
        Name = name,
        BrokerName = broker,
        PortfolioName = portfolio,
        BrokerStatus = "Active",
        ISIN = string.Empty,
        Exchange = string.Empty,
        Ticker = name,
        Country = CountryCode.Unknown,
        LocalTypeCode = string.Empty,
        Class = GlobalAssetClass.Unknown,
        Quantity = quantity,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesAssetsFromService()
    {
        var (viewModel, assetAdminService, _, _, _) = CreateViewModel();
        assetAdminService.Assets = [MakeAsset("BCIA11", "XPI", "Default", 100)];

        await viewModel.RefreshAsync();

        viewModel.Assets.Should().ContainSingle(a => a.Name == "BCIA11");
    }

    [Fact]
    public async Task RefreshAsync_FiltersDefaultToAllAndShowEveryAsset()
    {
        var (viewModel, assetAdminService, _, _, _) = CreateViewModel();
        assetAdminService.Assets = [MakeAsset("A", "XPI", "Default", 1), MakeAsset("B", "Avenue", "Growth", 0)];

        await viewModel.RefreshAsync();

        viewModel.Assets.Should().HaveCount(2);
    }

    [Fact]
    public async Task BrokerFilter_SetToOneBroker_ShowsOnlyThatBrokersAssets()
    {
        var (viewModel, assetAdminService, _, _, _) = CreateViewModel();
        assetAdminService.Assets = [MakeAsset("A", "XPI", "Default", 1), MakeAsset("B", "Avenue", "Growth", 0)];
        await viewModel.RefreshAsync();

        viewModel.BrokerFilter = "XPI";

        viewModel.Assets.Should().ContainSingle(a => a.Name == "A");
    }

    [Fact]
    public async Task CreateAssetAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, assetAdminService, _, portfolioService, dialog) = CreateViewModel();
        portfolioService.Portfolios = [new PortfolioDTO { Name = "Default", BrokerName = "XPI", BrokerStatus = "Active", AssetCount = 0 }];
        dialog.OnShowAssetFormDialog = vm => vm.Name = "NEWASSET";

        await viewModel.CreateAssetAsync();

        assetAdminService.LastCreateRequest.Should().NotBeNull();
        assetAdminService.LastCreateRequest!.BrokerName.Should().Be("XPI");
        assetAdminService.LastCreateRequest.PortfolioName.Should().Be("Default");
        assetAdminService.LastCreateRequest.Name.Should().Be("NEWASSET");
        viewModel.Assets.Should().ContainSingle(a => a.Name == "NEWASSET");
    }

    [Fact]
    public async Task CreateAssetAsync_ClassLeftAtUnknown_SendsNullClassToAutoResolve()
    {
        var (viewModel, assetAdminService, _, portfolioService, dialog) = CreateViewModel();
        portfolioService.Portfolios = [new PortfolioDTO { Name = "Default", BrokerName = "XPI", BrokerStatus = "Active", AssetCount = 0 }];
        dialog.OnShowAssetFormDialog = vm => vm.Name = "NEWASSET";

        await viewModel.CreateAssetAsync();

        assetAdminService.LastCreateRequest!.Class.Should().BeNull();
    }

    [Fact]
    public async Task CreateAssetAsync_ClassExplicitlySet_SendsThatClass()
    {
        var (viewModel, assetAdminService, _, portfolioService, dialog) = CreateViewModel();
        portfolioService.Portfolios = [new PortfolioDTO { Name = "Default", BrokerName = "XPI", BrokerStatus = "Active", AssetCount = 0 }];
        dialog.OnShowAssetFormDialog = vm =>
        {
            vm.Name = "NEWASSET";
            vm.Class = GlobalAssetClass.Equity;
        };

        await viewModel.CreateAssetAsync();

        assetAdminService.LastCreateRequest!.Class.Should().Be(GlobalAssetClass.Equity);
    }

    [Fact]
    public async Task CreateAssetAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, assetAdminService, _, _, dialog) = CreateViewModel();
        dialog.ShowAssetFormDialogResult = false;

        await viewModel.CreateAssetAsync();

        assetAdminService.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateAssetAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, assetAdminService, _, _, dialog) = CreateViewModel();
        dialog.OnShowAssetFormDialog = vm => vm.Name = "BCIA11";
        assetAdminService.ThrowOnCreate = new InvalidOperationException("Portfolio \"Default\" already has an asset named \"BCIA11\".");

        await viewModel.CreateAssetAsync();

        viewModel.ActionError.Should().Be("Portfolio \"Default\" already has an asset named \"BCIA11\".");
    }

    [Fact]
    public async Task EditAssetAsync_PreFillsDialogWithFixedBrokerPortfolioAndCallsUpdate()
    {
        var (viewModel, assetAdminService, _, _, dialog) = CreateViewModel();
        var asset = MakeAsset("BCIA11", "XPI", "Default", 100);
        dialog.OnShowAssetFormDialog = vm => vm.Name = "BCIA11B";

        await viewModel.EditAssetAsync(asset);

        dialog.LastAssetFormDialog!.BrokerName.Should().Be("XPI");
        dialog.LastAssetFormDialog.CanChangeBrokerPortfolio.Should().BeFalse();
        assetAdminService.LastUpdateRequest!.Value.BrokerName.Should().Be("XPI");
        assetAdminService.LastUpdateRequest.Value.PortfolioName.Should().Be("Default");
        assetAdminService.LastUpdateRequest.Value.CurrentName.Should().Be("BCIA11");
        assetAdminService.LastUpdateRequest.Value.Request.Name.Should().Be("BCIA11B");
    }

    [Fact]
    public async Task DeleteAssetAsync_NonZeroQuantity_SurfacesErrorWithoutConfirmingOrCallingService()
    {
        var (viewModel, _, assetMoveService, _, dialog) = CreateViewModel();
        var asset = MakeAsset("BCIA11", "XPI", "Default", 100);

        await viewModel.DeleteAssetAsync(asset);

        viewModel.ActionError.Should().Contain("still holds a position of 100");
        dialog.LastConfirmMessage.Should().BeNull();
        assetMoveService.LastArchiveRequest.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAssetAsync_ZeroQuantity_ConfirmsThenArchivesInPlace()
    {
        var (viewModel, _, assetMoveService, _, _) = CreateViewModel();
        var asset = MakeAsset("CLOSEDASSET", "XPI", "Uncategorized", 0);

        await viewModel.DeleteAssetAsync(asset);

        assetMoveService.LastArchiveRequest!.BrokerName.Should().Be("XPI");
        assetMoveService.LastArchiveRequest.SourcePortfolioName.Should().Be("Uncategorized");
        assetMoveService.LastArchiveRequest.AssetName.Should().Be("CLOSEDASSET");
        assetMoveService.LastArchiveRequest.DestinationPortfolioName.Should().Be("Uncategorized");
    }

    [Fact]
    public async Task DeleteAssetAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, _, assetMoveService, _, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var asset = MakeAsset("CLOSEDASSET", "XPI", "Uncategorized", 0);

        await viewModel.DeleteAssetAsync(asset);

        assetMoveService.LastArchiveRequest.Should().BeNull();
    }

    [Fact]
    public async Task EditAssetAsync_NullAsset_DoesNothing()
    {
        var (viewModel, assetAdminService, _, _, dialog) = CreateViewModel();

        await viewModel.EditAssetAsync(null);

        assetAdminService.LastUpdateRequest.Should().BeNull();
        dialog.LastAssetFormDialog.Should().BeNull();
    }

    [Fact]
    public async Task EditAssetAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, assetAdminService, _, _, dialog) = CreateViewModel();
        var asset = MakeAsset("BCIA11", "XPI", "Default", 100);
        dialog.OnShowAssetFormDialog = vm => vm.Name = "BCIA11B";
        assetAdminService.ThrowOnUpdate = new InvalidOperationException("Update failed.");

        await viewModel.EditAssetAsync(asset);

        viewModel.ActionError.Should().Be("Update failed.");
    }

    [Fact]
    public async Task DeleteAssetAsync_NullAsset_DoesNothing()
    {
        var (viewModel, _, assetMoveService, _, _) = CreateViewModel();

        await viewModel.DeleteAssetAsync(null);

        assetMoveService.LastArchiveRequest.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAssetAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, _, assetMoveService, _, _) = CreateViewModel();
        var asset = MakeAsset("CLOSEDASSET", "XPI", "Uncategorized", 0);
        assetMoveService.ThrowOnArchive = new InvalidOperationException("Archive failed.");

        await viewModel.DeleteAssetAsync(asset);

        viewModel.ActionError.Should().Be("Archive failed.");
    }

    [Fact]
    public async Task Properties_ExposeExpectedDefaultsAndCommands()
    {
        var (viewModel, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.IsLoading.Should().BeFalse();
        viewModel.HasError.Should().BeFalse();
        viewModel.RetryCommand.Should().NotBeNull();
        viewModel.CreateAssetCommand.Should().NotBeNull();
        viewModel.EditAssetCommand.Should().NotBeNull();
        viewModel.DeleteAssetCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task FilterOptions_AfterRefresh_IncludeAllOptionAndDistinctValues()
    {
        var (viewModel, assetAdminService, _, _, _) = CreateViewModel();
        assetAdminService.Assets = [MakeAsset("A", "XPI", "Default", 1), MakeAsset("B", "Avenue", "Growth", 0)];

        await viewModel.RefreshAsync();

        viewModel.BrokerFilterOptions.Should().Contain(["(All)", "XPI", "Avenue"]);
        viewModel.PortfolioFilterOptions.Should().Contain(["(All)", "Default", "Growth"]);
        viewModel.ClassFilterOptions.Should().Contain("(All)");
    }

    [Fact]
    public async Task PortfolioFilter_SetToOnePortfolio_ShowsOnlyThatPortfoliosAssets()
    {
        var (viewModel, assetAdminService, _, _, _) = CreateViewModel();
        assetAdminService.Assets = [MakeAsset("A", "XPI", "Default", 1), MakeAsset("B", "Avenue", "Growth", 0)];
        await viewModel.RefreshAsync();

        viewModel.PortfolioFilter = "Growth";

        viewModel.Assets.Should().ContainSingle(a => a.Name == "B");
    }

    [Fact]
    public async Task ClassFilter_SetToOneClass_ShowsOnlyAssetsOfThatClass()
    {
        var (viewModel, assetAdminService, _, _, _) = CreateViewModel();
        var equityAsset = MakeAsset("A", "XPI", "Default", 1);
        equityAsset.Class = GlobalAssetClass.Equity;
        assetAdminService.Assets = [equityAsset, MakeAsset("B", "Avenue", "Growth", 0)];
        await viewModel.RefreshAsync();

        viewModel.ClassFilter = GlobalAssetClass.Equity.ToString();

        viewModel.Assets.Should().ContainSingle(a => a.Name == "A");
    }

    [Fact]
    public async Task EditAssetAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, assetAdminService, _, _, dialog) = CreateViewModel();
        var asset = MakeAsset("BCIA11", "XPI", "Default", 100);
        dialog.ShowAssetFormDialogResult = false;

        await viewModel.EditAssetAsync(asset);

        assetAdminService.LastUpdateRequest.Should().BeNull();
    }
}
