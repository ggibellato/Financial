using System.Windows;
using System.Windows.Controls;

namespace Financial.Presentation.App.Controls;

/// <summary>
/// Reusable contextual-help affordance (P38-F02): a small Info icon button that opens a Flyout
/// with a short explanation. Set <see cref="HelpText"/> next to the field it describes.
/// </summary>
public partial class HelpFlyoutButton : UserControl
{
    public static readonly DependencyProperty HelpTextProperty = DependencyProperty.Register(
        nameof(HelpText), typeof(string), typeof(HelpFlyoutButton), new PropertyMetadata(string.Empty));

    public string HelpText
    {
        get => (string)GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    public HelpFlyoutButton()
    {
        InitializeComponent();
    }

    private void OnInfoButtonClick(object sender, RoutedEventArgs e)
    {
        HelpFlyout.IsOpen = !HelpFlyout.IsOpen;
    }
}
