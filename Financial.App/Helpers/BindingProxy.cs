using System.Windows;

namespace Financial.Presentation.App.Helpers;

/// <summary>Freezable proxy that lets a XAML resource carry a DataContext-scoped binding value
/// into places outside the visual tree (e.g. DataGridColumn), where a direct RelativeSource or
/// x:Reference binding either can't resolve or causes a MarkupExtension cyclical-dependency error.</summary>
public sealed class BindingProxy : Freezable
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(object), typeof(BindingProxy), new PropertyMetadata(null));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
