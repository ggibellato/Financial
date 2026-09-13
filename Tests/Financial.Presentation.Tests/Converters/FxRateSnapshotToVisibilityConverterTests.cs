using System.Globalization;
using System.Windows;
using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class FxRateSnapshotToVisibilityConverterTests
{
    private readonly FxRateSnapshotToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_Null_ReturnsCollapsed()
    {
        _converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_FxRateSnapshotDto_ReturnsVisible()
    {
        var snapshot = new FxRateSnapshotDTO { ToCurrency = "GBP", Rate = 5.1m, Source = "Frankfurter", RetrievedAt = DateTimeOffset.UtcNow };

        _converter.Convert(snapshot, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Visible);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var act = () => _converter.ConvertBack(Visibility.Visible, typeof(FxRateSnapshotDTO), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
