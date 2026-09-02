using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Financial.Presentation.App.Controls;

/// <summary>Carries a row's identity through to the command, since <see cref="StatusSplitButton"/> is
/// generic over what "Bill" means and does not know the DTO shape itself.</summary>
public sealed record StatusChangeRequest(object? Bill, string NewStatus);

public partial class StatusSplitButton : UserControl
{
    public static readonly DependencyProperty StatusesProperty = DependencyProperty.Register(
        nameof(Statuses), typeof(IEnumerable<string>), typeof(StatusSplitButton), new PropertyMetadata(null));

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(string), typeof(StatusSplitButton), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BillProperty = DependencyProperty.Register(
        nameof(Bill), typeof(object), typeof(StatusSplitButton), new PropertyMetadata(null));

    public static readonly DependencyProperty ChangeStatusCommandProperty = DependencyProperty.Register(
        nameof(ChangeStatusCommand), typeof(ICommand), typeof(StatusSplitButton), new PropertyMetadata(null));

    public IEnumerable<string>? Statuses
    {
        get => (IEnumerable<string>?)GetValue(StatusesProperty);
        set => SetValue(StatusesProperty, value);
    }

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public object? Bill
    {
        get => GetValue(BillProperty);
        set => SetValue(BillProperty, value);
    }

    public ICommand? ChangeStatusCommand
    {
        get => (ICommand?)GetValue(ChangeStatusCommandProperty);
        set => SetValue(ChangeStatusCommandProperty, value);
    }

    public StatusSplitButton()
    {
        InitializeComponent();

        // A ContextMenu is not part of the visual tree it's declared in - it gets its own
        // NameScope and does not inherit ambient DataContext, so {Binding ElementName=root}
        // inside it silently resolves to nothing (confirmed: it left ItemsSource null). Setting
        // DataContext explicitly here is the standard WPF fix; the XAML then binds against it
        // via RelativeSource AncestorType=ContextMenu instead of ElementName.
        if (Split.ContextMenu is { } contextMenu)
        {
            contextMenu.DataContext = this;
        }
    }

    private void OnSplitButtonClick(object sender, RoutedEventArgs e)
    {
        if (Split.ContextMenu is not { } contextMenu)
        {
            return;
        }

        // Release any capture DataGridCell's own row-selection handling might still hold from
        // this same click, so the ContextMenu (and its items) get uncontested capture once open -
        // the same DataGridCell/mouse-capture conflict documented on the XAML side for why this
        // control uses a plain Button instead of SplitButton in the first place.
        Mouse.Capture(null);

        contextMenu.PlacementTarget = Split;
        contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        contextMenu.IsOpen = true;
    }

    private void OnStatusItemClick(object sender, RoutedEventArgs e)
    {
        // Tag (not DataContext) carries the status: these are static XAML MenuItems, not
        // ItemsSource-generated containers, so DataContext is inherited from the ContextMenu
        // (the StatusSplitButton instance itself) rather than being the per-item status string.
        if (sender is not FrameworkElement { Tag: string newStatus })
        {
            return;
        }

        // The ContextMenu closes itself on any item click - nothing else to do here.
        ChangeStatusCommand?.Execute(new StatusChangeRequest(Bill, newStatus));
    }
}
