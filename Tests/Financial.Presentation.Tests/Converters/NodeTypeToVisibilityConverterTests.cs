using System.Globalization;
using System.Windows;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

[Trait("Category", "Unit")]
public class NodeTypeToVisibilityConverterTests
{
    private readonly NodeTypeToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_AssetNodeType_ReturnsVisible()
    {
        _converter.Convert("Asset", typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_NonAssetNodeType_ReturnsCollapsed()
    {
        _converter.Convert("Broker", typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_NonStringValue_ReturnsCollapsed()
    {
        _converter.Convert(42, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
    }
}
