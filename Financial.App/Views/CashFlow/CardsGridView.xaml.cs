using System.Windows;
using System.Windows.Controls;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class CardsGridView : UserControl
{
    public static readonly DependencyProperty ShowCardManagementColumnsProperty = DependencyProperty.Register(
        nameof(ShowCardManagementColumns), typeof(bool), typeof(CardsGridView), new PropertyMetadata(false));

    public bool ShowCardManagementColumns
    {
        get => (bool)GetValue(ShowCardManagementColumnsProperty);
        set => SetValue(ShowCardManagementColumnsProperty, value);
    }

    public CardsGridView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // DataGridColumn isn't part of the visual tree, so its Header can't bind to the ambient
    // DataContext - it's assigned directly to the filter ViewModel instance here instead, and
    // renders through the FilterableColumnHeader implicit DataTemplate in App.xaml.
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is CardsWorkflowViewModel viewModel)
        {
            CardColumn.Header = viewModel.CardFilter;
        }
    }

    private void OnBankComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: BankDTO bank } comboBox)
        {
            return;
        }

        if (comboBox.DataContext is not CreditCardManagementRow { Statement: { } statement } || DataContext is not CardsWorkflowViewModel viewModel)
        {
            return;
        }

        viewModel.SetMarkPaidSource(statement.Id, bank.Id);
    }

    private void OnDueDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DatePicker { DataContext: CreditCardManagementRow row } picker)
        {
            return;
        }

        if (DataContext is not CardsWorkflowViewModel viewModel)
        {
            return;
        }

        var newDueDate = picker.SelectedDate.HasValue ? DateOnly.FromDateTime(picker.SelectedDate.Value) : (DateOnly?)null;
        _ = viewModel.UpdateCreditCardAsync(row.CreditCard, newDueDate, row.CreditCard.IsActive);
    }

    private void OnActiveChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { DataContext: CreditCardManagementRow row } checkBox)
        {
            return;
        }

        if (DataContext is not CardsWorkflowViewModel viewModel)
        {
            return;
        }

        _ = viewModel.UpdateCreditCardAsync(row.CreditCard, row.CreditCard.NextInvoiceDueDate, checkBox.IsChecked == true);
    }
}
