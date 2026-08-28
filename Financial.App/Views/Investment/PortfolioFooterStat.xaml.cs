using System.Windows;
using System.Windows.Controls;

namespace Financial.Presentation.App.Views.Investment;

public partial class PortfolioFooterStat : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(PortfolioFooterStat));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(PortfolioFooterStat));

    public PortfolioFooterStat()
    {
        InitializeComponent();
    }

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
