using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Maps a credit card's calendar sync state to background/foreground colors, mirroring
/// <see cref="BillStatusToBrushConverter"/>'s shape. "Pending" and an absent state (a card
/// never synced this process lifetime) both render as the same neutral "still syncing" look.
/// </summary>
public class CalendarSyncStateToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush PendingBackground = Freeze(0xEB, 0xEB, 0xEB);
    private static readonly SolidColorBrush PendingForeground = Freeze(0x61, 0x61, 0x61);
    private static readonly SolidColorBrush SyncedBackground = Freeze(0x10, 0x7C, 0x10);
    private static readonly SolidColorBrush SyncedForeground = Freeze(0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush ErrorBackground = Freeze(0xD1, 0x34, 0x38);
    private static readonly SolidColorBrush ErrorForeground = Freeze(0xFF, 0xFF, 0xFF);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isBackground = string.Equals(parameter as string, "Background", StringComparison.OrdinalIgnoreCase);

        return (value as string) switch
        {
            "Synced" => isBackground ? SyncedBackground : SyncedForeground,
            "Error" => isBackground ? ErrorBackground : ErrorForeground,
            _ => isBackground ? PendingBackground : PendingForeground,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
