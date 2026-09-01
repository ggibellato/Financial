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
    }

    private void OnStatusItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: string newStatus })
        {
            return;
        }

        Split.IsDropDownOpen = false;
        ChangeStatusCommand?.Execute(new StatusChangeRequest(Bill, newStatus));
    }
}
