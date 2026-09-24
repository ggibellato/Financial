using System.Windows;
using System.Windows.Controls;

namespace Financial.Presentation.App.Controls;

public partial class TargetAssetPickerControl : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(TargetAssetPickerControl), new PropertyMetadata("Target Asset"));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public TargetAssetPickerControl()
    {
        InitializeComponent();
    }
}
