using System.Windows;
using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class AssetActionsBaseTests
{
    private sealed class TestAssetActions(
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage,
        string title)
        : AssetActionsBase(hasContext, brokerName, portfolioName, assetName, applyDetails, showMessage, title)
    {
        public bool CallHasContext() => HasContext();
        public string CallGetBrokerName() => GetBrokerName();
        public string CallGetPortfolioName() => GetPortfolioName();
        public string CallGetAssetName() => GetAssetName();
        public void CallApplyDetails(AssetDetailsDTO details) => ApplyDetails(details);
        public void CallShowInfo(string message) => ShowInfo(message);
        public void CallShowWarning(string message) => ShowWarning(message);
    }

    private static AssetDetailsDTO CreateDetails() => new()
    {
        Name = "PETR4",
        BrokerName = "XP",
        PortfolioName = "Long Term",
        Ticker = "PETR4",
    };

    private static TestAssetActions CreateActions(
        Func<bool>? hasContext = null,
        Action<AssetDetailsDTO>? applyDetails = null,
        Action<string, string, MessageBoxImage>? showMessage = null) =>
        new(
            hasContext ?? (() => true),
            () => "XP",
            () => "Long Term",
            () => "PETR4",
            applyDetails ?? (_ => { }),
            showMessage ?? ((_, _, _) => { }),
            "Asset Actions");

    [Fact]
    public void Constructor_NullHasContext_Throws()
    {
        var act = () => new TestAssetActions(null!, () => "b", () => "p", () => "a", _ => { }, (_, _, _) => { }, "t");

        act.Should().Throw<ArgumentNullException>().WithParameterName("hasContext");
    }

    [Fact]
    public void Constructor_NullBrokerName_Throws()
    {
        var act = () => new TestAssetActions(() => true, null!, () => "p", () => "a", _ => { }, (_, _, _) => { }, "t");

        act.Should().Throw<ArgumentNullException>().WithParameterName("brokerName");
    }

    [Fact]
    public void Constructor_NullPortfolioName_Throws()
    {
        var act = () => new TestAssetActions(() => true, () => "b", null!, () => "a", _ => { }, (_, _, _) => { }, "t");

        act.Should().Throw<ArgumentNullException>().WithParameterName("portfolioName");
    }

    [Fact]
    public void Constructor_NullAssetName_Throws()
    {
        var act = () => new TestAssetActions(() => true, () => "b", () => "p", null!, _ => { }, (_, _, _) => { }, "t");

        act.Should().Throw<ArgumentNullException>().WithParameterName("assetName");
    }

    [Fact]
    public void Constructor_NullApplyDetails_Throws()
    {
        var act = () => new TestAssetActions(() => true, () => "b", () => "p", () => "a", null!, (_, _, _) => { }, "t");

        act.Should().Throw<ArgumentNullException>().WithParameterName("applyDetails");
    }

    [Fact]
    public void Constructor_NullShowMessage_Throws()
    {
        var act = () => new TestAssetActions(() => true, () => "b", () => "p", () => "a", _ => { }, null!, "t");

        act.Should().Throw<ArgumentNullException>().WithParameterName("showMessage");
    }

    [Fact]
    public void HasContext_ForwardsToDelegate()
    {
        var actions = CreateActions(hasContext: () => false);

        actions.CallHasContext().Should().BeFalse();
    }

    [Fact]
    public void GetBrokerName_ForwardsToDelegate()
    {
        var actions = CreateActions();

        actions.CallGetBrokerName().Should().Be("XP");
    }

    [Fact]
    public void GetPortfolioName_ForwardsToDelegate()
    {
        var actions = CreateActions();

        actions.CallGetPortfolioName().Should().Be("Long Term");
    }

    [Fact]
    public void GetAssetName_ForwardsToDelegate()
    {
        var actions = CreateActions();

        actions.CallGetAssetName().Should().Be("PETR4");
    }

    [Fact]
    public void ApplyDetails_ForwardsDetailsToDelegate()
    {
        AssetDetailsDTO? received = null;
        var actions = CreateActions(applyDetails: d => received = d);
        var details = CreateDetails();

        actions.CallApplyDetails(details);

        received.Should().BeSameAs(details);
    }

    [Fact]
    public void ShowInfo_ForwardsMessageTitleAndInformationImage()
    {
        string? message = null, title = null;
        MessageBoxImage? image = null;
        var actions = CreateActions(showMessage: (m, t, i) => { message = m; title = t; image = i; });

        actions.CallShowInfo("saved");

        message.Should().Be("saved");
        title.Should().Be("Asset Actions");
        image.Should().Be(MessageBoxImage.Information);
    }

    [Fact]
    public void ShowWarning_ForwardsMessageTitleAndWarningImage()
    {
        MessageBoxImage? image = null;
        var actions = CreateActions(showMessage: (_, _, i) => image = i);

        actions.CallShowWarning("careful");

        image.Should().Be(MessageBoxImage.Warning);
    }
}
