using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class BillTableView : UserControl
{
    public static readonly DependencyProperty BillsProperty = DependencyProperty.Register(
        nameof(Bills), typeof(IEnumerable), typeof(BillTableView), new PropertyMetadata(null));

    public static readonly DependencyProperty ShowBrasilFieldsProperty = DependencyProperty.Register(
        nameof(ShowBrasilFields), typeof(bool), typeof(BillTableView), new PropertyMetadata(false));

    public static readonly DependencyProperty EditBillCommandProperty = DependencyProperty.Register(
        nameof(EditBillCommand), typeof(ICommand), typeof(BillTableView), new PropertyMetadata(null));

    public static readonly DependencyProperty DeleteBillCommandProperty = DependencyProperty.Register(
        nameof(DeleteBillCommand), typeof(ICommand), typeof(BillTableView), new PropertyMetadata(null));

    public static readonly DependencyProperty ChangeStatusCommandProperty = DependencyProperty.Register(
        nameof(ChangeStatusCommand), typeof(ICommand), typeof(BillTableView), new PropertyMetadata(null));

    public IEnumerable? Bills
    {
        get => (IEnumerable?)GetValue(BillsProperty);
        set => SetValue(BillsProperty, value);
    }

    public bool ShowBrasilFields
    {
        get => (bool)GetValue(ShowBrasilFieldsProperty);
        set => SetValue(ShowBrasilFieldsProperty, value);
    }

    public ICommand? EditBillCommand
    {
        get => (ICommand?)GetValue(EditBillCommandProperty);
        set => SetValue(EditBillCommandProperty, value);
    }

    public ICommand? DeleteBillCommand
    {
        get => (ICommand?)GetValue(DeleteBillCommandProperty);
        set => SetValue(DeleteBillCommandProperty, value);
    }

    public ICommand? ChangeStatusCommand
    {
        get => (ICommand?)GetValue(ChangeStatusCommandProperty);
        set => SetValue(ChangeStatusCommandProperty, value);
    }

    public BillTableView()
    {
        InitializeComponent();
    }
}
