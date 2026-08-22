using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Presentation.Tests.ViewModels;

public class TreeNodeViewModelTests
{
    private static TreeNodeDTO BuildDto(string name, TreeNodeType nodeType, Dictionary<string, object>? metadata = null, List<TreeNodeDTO>? children = null) =>
        new()
        {
            DisplayName = name,
            NodeType = nodeType,
            Metadata = metadata ?? new Dictionary<string, object>(),
            Children = children ?? []
        };

    [Fact]
    public void Constructor_MapsDisplayNameNodeTypeAndMetadataFromDto()
    {
        var dto = BuildDto("XPI", TreeNodeType.Broker, new Dictionary<string, object> { ["Currency"] = "BRL" });

        var node = new TreeNodeViewModel(dto);

        node.DisplayName.Should().Be("XPI");
        node.NodeType.Should().Be(TreeNodeType.Broker);
        node.Metadata.Should().ContainKey("Currency").WhoseValue.Should().Be("BRL");
        node.Parent.Should().BeNull();
    }

    [Fact]
    public void Constructor_RecursivelyBuildsChildrenWithParentSet()
    {
        var childDto = BuildDto("Default", TreeNodeType.Portfolio);
        var rootDto = BuildDto("XPI", TreeNodeType.Broker, children: [childDto]);

        var root = new TreeNodeViewModel(rootDto);

        root.Children.Should().ContainSingle();
        var child = root.Children[0];
        child.DisplayName.Should().Be("Default");
        child.Parent.Should().BeSameAs(root);
    }

    [Fact]
    public void Constructor_BuildsMultipleLevelsOfNesting()
    {
        var assetDto = BuildDto("PETR4", TreeNodeType.Asset);
        var portfolioDto = BuildDto("Default", TreeNodeType.Portfolio, children: [assetDto]);
        var brokerDto = BuildDto("XPI", TreeNodeType.Broker, children: [portfolioDto]);

        var root = new TreeNodeViewModel(brokerDto);

        var portfolio = root.Children.Single();
        var asset = portfolio.Children.Single();
        asset.DisplayName.Should().Be("PETR4");
        asset.Parent.Should().BeSameAs(portfolio);
        portfolio.Parent.Should().BeSameAs(root);
    }

    [Fact]
    public void PositionType_ForAnAssetNode_ReadsTheMetadata()
    {
        var node = new TreeNodeViewModel(
            BuildDto("PETR4", TreeNodeType.Asset, new Dictionary<string, object> { ["PositionType"] = "Long" }));

        node.PositionType.Should().Be("Long");
    }

    [Fact]
    public void PositionType_ForANodeWithoutOne_IsEmptyRatherThanThrowing()
    {
        // Every broker and portfolio row lands here. Binding Metadata[PositionType] instead threw
        // KeyNotFoundException on each one, which WPF caught and logged as a binding error.
        var broker = new TreeNodeViewModel(BuildDto("XPI", TreeNodeType.Broker));
        var portfolio = new TreeNodeViewModel(BuildDto("Default", TreeNodeType.Portfolio));

        using (new AssertionScope())
        {
            broker.PositionType.Should().BeEmpty();
            portfolio.PositionType.Should().BeEmpty();
        }
    }

    [Fact]
    public void PositionType_WhenTheMetadataIsNotAString_IsEmpty()
    {
        var node = new TreeNodeViewModel(
            BuildDto("PETR4", TreeNodeType.Asset, new Dictionary<string, object> { ["PositionType"] = 42 }));

        node.PositionType.Should().BeEmpty();
    }

    [Fact]
    public void GetMetadata_ExistingKeyWithMatchingType_ReturnsValue()
    {
        var dto = BuildDto("PETR4", TreeNodeType.Asset, new Dictionary<string, object> { ["PositionType"] = "Long" });
        var node = new TreeNodeViewModel(dto);

        node.GetMetadata<string>("PositionType").Should().Be("Long");
    }

    [Fact]
    public void GetMetadata_MissingKey_ReturnsDefaultValue()
    {
        var dto = BuildDto("PETR4", TreeNodeType.Asset);
        var node = new TreeNodeViewModel(dto);

        node.GetMetadata("PositionType", "Flat").Should().Be("Flat");
    }

    [Fact]
    public void GetMetadata_KeyPresentWithMismatchedType_ReturnsDefaultValue()
    {
        var dto = BuildDto("PETR4", TreeNodeType.Asset, new Dictionary<string, object> { ["PositionType"] = 42 });
        var node = new TreeNodeViewModel(dto);

        node.GetMetadata("PositionType", "Flat").Should().Be("Flat");
    }

    [Fact]
    public void IsSelected_SetTrue_RaisesNodeSelectedOnItself()
    {
        var node = new TreeNodeViewModel(BuildDto("XPI", TreeNodeType.Broker));
        TreeNodeViewModel? selectedNode = null;
        node.NodeSelected += (_, selected) => selectedNode = selected;

        node.IsSelected = true;

        selectedNode.Should().BeSameAs(node);
    }

    [Fact]
    public void IsSelected_SetFalse_DoesNotRaiseNodeSelected()
    {
        var node = new TreeNodeViewModel(BuildDto("XPI", TreeNodeType.Broker));
        var raised = false;
        node.NodeSelected += (_, _) => raised = true;

        node.IsSelected = false;

        raised.Should().BeFalse();
    }

    [Fact]
    public void IsSelected_OnChild_BubblesNodeSelectedEventUpToParent()
    {
        var childDto = BuildDto("Default", TreeNodeType.Portfolio);
        var rootDto = BuildDto("XPI", TreeNodeType.Broker, children: [childDto]);
        var root = new TreeNodeViewModel(rootDto);
        var child = root.Children[0];
        TreeNodeViewModel? selectedOnParent = null;
        root.NodeSelected += (_, selected) => selectedOnParent = selected;

        child.IsSelected = true;

        selectedOnParent.Should().BeSameAs(child);
    }

    [Fact]
    public void IsSelected_OnGrandchild_BubblesNodeSelectedEventThroughEveryAncestor()
    {
        var assetDto = BuildDto("PETR4", TreeNodeType.Asset);
        var portfolioDto = BuildDto("Default", TreeNodeType.Portfolio, children: [assetDto]);
        var brokerDto = BuildDto("XPI", TreeNodeType.Broker, children: [portfolioDto]);
        var root = new TreeNodeViewModel(brokerDto);
        var portfolio = root.Children[0];
        var asset = portfolio.Children[0];
        TreeNodeViewModel? selectedOnRoot = null;
        TreeNodeViewModel? selectedOnPortfolio = null;
        root.NodeSelected += (_, selected) => selectedOnRoot = selected;
        portfolio.NodeSelected += (_, selected) => selectedOnPortfolio = selected;

        asset.IsSelected = true;

        selectedOnRoot.Should().BeSameAs(asset);
        selectedOnPortfolio.Should().BeSameAs(asset);
    }

    [Fact]
    public void IsExpanded_SetToNewValue_RaisesPropertyChanged()
    {
        var node = new TreeNodeViewModel(BuildDto("XPI", TreeNodeType.Broker));
        var raisedProperties = new List<string?>();
        node.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName);

        node.IsExpanded = true;

        raisedProperties.Should().Contain(nameof(TreeNodeViewModel.IsExpanded));
    }
}
