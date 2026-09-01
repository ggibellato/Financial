using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Maps a Mensais bill status to the same background/foreground colors already shipping in the
/// React status tag (Financial.Web's StatusMenuButton), sampled from the running app rather than
/// a theoretical Fluent token, per docs/ui/decisions/ADR-005's guidance to match rendered pixels.
/// </summary>
public class BillStatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush UnsetBackground = Freeze(0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush UnsetForeground = Freeze(0x24, 0x24, 0x24);
    private static readonly SolidColorBrush ScheduledBackground = Freeze(0xEB, 0xEB, 0xEB);
    private static readonly SolidColorBrush ScheduledForeground = Freeze(0x61, 0x61, 0x61);
    private static readonly SolidColorBrush PaidBackground = Freeze(0x10, 0x7C, 0x10);
    private static readonly SolidColorBrush PaidForeground = Freeze(0xFF, 0xFF, 0xFF);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isBackground = string.Equals(parameter as string, "Background", StringComparison.OrdinalIgnoreCase);

        return (value as string) switch
        {
            "Paid" => isBackground ? PaidBackground : PaidForeground,
            "Scheduled" => isBackground ? ScheduledBackground : ScheduledForeground,
            _ => isBackground ? UnsetBackground : UnsetForeground,
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
