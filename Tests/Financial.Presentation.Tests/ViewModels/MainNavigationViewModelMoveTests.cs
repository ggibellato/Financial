using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Exceptions;
using System.IO;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using FluentAssertions.Execution;

// The doubles these tests need already exist next door; reuse beats a second copy.
using SpyAssetDetailsViewModel = Financial.Presentation.Tests.ViewModels.MainNavigationViewModelBaseTests.SpyAssetDetailsViewModel;
using StubNavigationService = Financial.Presentation.Tests.ViewModels.MainNavigationViewModelBaseTests.StubNavigationService;
using StubSummaryService = Financial.Presentation.Tests.ViewModels.MainNavigationViewModelBaseTests.StubSummaryService;
using TestableNavigationViewModel = Financial.Presentation.Tests.ViewModels.MainNavigationViewModelBaseTests.TestableNavigationViewModel;

namespace Financial.Presentation.Tests.ViewModels;

/// <summary>
/// The move command on the navigation view model: what it sends, what it does with a refusal, and
/// that the tree ends up showing the asset where it landed.
/// </summary>
public class MainNavigationViewModelMoveTests
{
    private readonly StubAssetMoveService _moveService = new();

    [Fact]
    public async Task MoveSelectedAssetAsync_SendsTheSelectedAssetAndTheChosenDestination()
    {
        var sut = CreateViewModel();
        await SelectAssetAsync(sut, "AAAA");
        sut.MoveDialogResponse = dialog =>
        {
            dialog.SelectedPortfolioName = "ISA";
            return true;
        };

        await sut.MoveSelectedAssetAsync();

        using (new AssertionScope())
        {
            _moveService.LastRequest.Should().NotBeNull();
            _moveService.LastRequest!.BrokerName.Should().Be("XPI");
            _moveService.LastRequest.SourcePortfolioName.Should().Be("Default");
            _moveService.LastRequest.AssetName.Should().Be("AAAA");
            _moveService.LastRequest.DestinationPortfolioName.Should().Be("ISA");
            _moveService.LastRequest.Scope.Should().Be("Active");
        }
    }

    [Fact]
    public async Task MoveSelectedAssetAsync_OffersTheBrokersOtherPortfoliosAsDestinations()
    {
        var sut = CreateViewModel();
        await SelectAssetAsync(sut, "AAAA");

        await sut.MoveSelectedAssetAsync();

        sut.LastMoveDialog!.AvailablePortfolios.Should().BeEquivalentTo(["ISA"]);
    }

    [Fact]
    public async Task MoveSelectedAssetAsync_WhenTheDialogIsCancelled_SendsNothing()
    {
        var sut = CreateViewModel();
        await SelectAssetAsync(sut, "AAAA");
        sut.MoveDialogResponse = _ => false;

        await sut.MoveSelectedAssetAsync();

        _moveService.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task MoveSelectedAssetAsync_AfterMoving_ReloadsTheTreeAndSelectsTheAssetWhereItLanded()
    {
        var sut = CreateViewModel();
        await SelectAssetAsync(sut, "AAAA");
        sut.MoveDialogResponse = dialog =>
        {
            dialog.SelectedPortfolioName = "ISA";
            return true;
        };

        // What the tree looks like once the move has been saved.
        _moveService.OnMove = () => sut.NavigationService.Tree = BuildTree(assetPortfolio: "ISA");

        await sut.MoveSelectedAssetAsync();

        using (new AssertionScope())
        {
            sut.SelectedNode.Should().NotBeNull();
            sut.SelectedNode!.GetMetadata<string>("AssetName").Should().Be("AAAA");
            sut.SelectedNode.Parent!.GetMetadata<string>("PortfolioName").Should().Be("ISA");
        }
    }

    [Fact]
    public async Task MoveSelectedAssetAsync_WhenRefused_ShowsTheDomainsReasonUnchanged()
    {
        // The message travels from the domain so the desktop app and the web app say the same thing.
        const string reason = "Portfolio \"ISA\" already holds an asset named \"AAAA\".";
        var sut = CreateViewModel();
        await SelectAssetAsync(sut, "AAAA");
        sut.MoveDialogResponse = dialog => { dialog.SelectedPortfolioName = "ISA"; return true; };
        _moveService.Failure = new InvestmentRuleViolationException(reason);

        await sut.MoveSelectedAssetAsync();

        sut.LastMoveFailureMessage.Should().Be(reason);
    }

    [Fact]
    public async Task MoveSelectedAssetAsync_WhenNotFound_ShowsTheReason()
    {
        var sut = CreateViewModel();
        await SelectAssetAsync(sut, "AAAA");
        sut.MoveDialogResponse = dialog => { dialog.SelectedPortfolioName = "ISA"; return true; };
        _moveService.Failure = new KeyNotFoundException("Asset \"AAAA\" was not found.");

        await sut.MoveSelectedAssetAsync();

        sut.LastMoveFailureMessage.Should().Contain("was not found");
    }

    [Fact]
    public async Task MoveSelectedAssetAsync_WhenTheSaveFails_ReportsItRatherThanEscaping()
    {
        // The command is invoked as async void, so an escaping exception would take the process
        // down - and a storage fault is likelier than a domain refusal on a Drive-backed install.
        var sut = CreateViewModel();
        await SelectAssetAsync(sut, "AAAA");
        sut.MoveDialogResponse = dialog => { dialog.SelectedPortfolioName = "ISA"; return true; };
        _moveService.Failure = new IOException("the network drive went away");

        await sut.MoveSelectedAssetAsync();

        sut.LastMoveFailureMessage.Should().Contain(nameof(IOException));
    }

    [Fact]
    public async Task SelectingAnAssetInTheTree_MakesItTheSelectedNode()
    {
        // The regression: clicking a node used to load the detail panel without ever setting
        // SelectedNode, so every command reading it stayed disabled no matter what the user picked.
        var sut = CreateViewModel();
        await sut.LoadNavigationTreeAsync();

        AssetNode(sut, "AAAA").IsSelected = true;

        using (new AssertionScope())
        {
            sut.SelectedNode.Should().NotBeNull();
            sut.SelectedNode!.GetMetadata<string>("AssetName").Should().Be("AAAA");
            sut.MoveAssetCommand.CanExecute(null).Should().BeTrue();
        }
    }

    [Fact]
    public async Task MoveSelectedAssetAsync_AfterMoving_HighlightsTheMovedAssetInTheTree()
    {
        // Not just current in the view model - actually selected in the tree the user is looking at.
        var sut = CreateViewModel();
        await SelectAssetAsync(sut, "AAAA");
        sut.MoveDialogResponse = dialog => { dialog.SelectedPortfolioName = "ISA"; return true; };
        _moveService.OnMove = () => sut.NavigationService.Tree = BuildTree(assetPortfolio: "ISA");

        await sut.MoveSelectedAssetAsync();

        AssetNode(sut, "AAAA").IsSelected.Should().BeTrue();
    }

    [Fact]
    public async Task MoveAssetCommand_IsUnavailableUntilAnAssetIsSelected()
    {
        var sut = CreateViewModel();

        sut.MoveAssetCommand.CanExecute(null).Should().BeFalse();

        await SelectAssetAsync(sut, "AAAA");

        sut.MoveAssetCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task MoveSelectedAssetAsync_WithAPortfolioSelected_DoesNothing()
    {
        var sut = CreateViewModel();
        await sut.LoadNavigationTreeAsync();
        sut.RootNodes[0].Children[0].IsSelected = true;

        await sut.MoveSelectedAssetAsync();

        _moveService.LastRequest.Should().BeNull();
    }

    private TestableNavigationViewModel CreateViewModel()
    {
        var navigationService = new StubNavigationService { Tree = BuildTree(assetPortfolio: "Default") };
        return new TestableNavigationViewModel(
            new StubSummaryService(),
            new SpyAssetDetailsViewModel(),
            _navigationService: navigationService,
            assetMoveService: _moveService);
    }

    /// <summary>
    /// Selects through IsSelected, which is the only route the UI has - the container style binds
    /// it two-way and the resulting NodeSelected event is what reaches the view model. Assigning
    /// SelectedNode directly, as these tests used to, exercises a path no click can take, and hid
    /// the fact that the move command could never enable in the running app.
    /// </summary>
    private static async Task SelectAssetAsync(TestableNavigationViewModel sut, string assetName)
    {
        await sut.LoadNavigationTreeAsync();
        AssetNode(sut, assetName).IsSelected = true;
    }

    private static TreeNodeViewModel AssetNode(TestableNavigationViewModel sut, string assetName) =>
        sut.RootNodes
            .SelectMany(broker => broker.Children)
            .SelectMany(portfolio => portfolio.Children)
            .First(asset => asset.GetMetadata<string>("AssetName") == assetName);

    /// <summary>Broker XPI with portfolios "Default" and "ISA"; the asset sits in whichever is named.</summary>
    private static TreeNodeDTO BuildTree(string assetPortfolio)
    {
        TreeNodeDTO Portfolio(string name) => new()
        {
            NodeType = TreeNodeType.Portfolio,
            DisplayName = name,
            Metadata = new Dictionary<string, object> { ["PortfolioName"] = name },
            Children = name == assetPortfolio
                ?
                [
                    new TreeNodeDTO
                    {
                        NodeType = TreeNodeType.Asset,
                        DisplayName = "AAAA",
                        Metadata = new Dictionary<string, object> { ["AssetName"] = "AAAA" },
                        Children = []
                    }
                ]
                : []
        };

        return new TreeNodeDTO
        {
            NodeType = TreeNodeType.Investments,
            DisplayName = "Root",
            Metadata = [],
            Children =
            [
                new TreeNodeDTO
                {
                    NodeType = TreeNodeType.Broker,
                    DisplayName = "XPI",
                    Metadata = new Dictionary<string, object> { ["BrokerName"] = "XPI" },
                    Children = [Portfolio("Default"), Portfolio("ISA")]
                }
            ]
        };
    }

    private sealed class StubAssetMoveService : IAssetMoveService
    {
        public MoveAssetRequestDTO? LastRequest { get; private set; }
        public Exception? Failure { get; set; }
        public Action? OnMove { get; set; }

        public Task<AssetDetailsDTO> ArchiveAssetAsync(ArchiveAssetRequestDTO request) => throw new NotImplementedException();

        public Task<AssetDetailsDTO> MoveAssetAsync(MoveAssetRequestDTO request)
        {
            if (Failure is not null)
            {
                return Task.FromException<AssetDetailsDTO>(Failure);
            }

            LastRequest = request;
            OnMove?.Invoke();

            return Task.FromResult(new AssetDetailsDTO
            {
                Name = request.AssetName,
                BrokerName = request.BrokerName,
                PortfolioName = request.DestinationPortfolioName,
                Ticker = request.AssetName
            });
        }
    }
}
