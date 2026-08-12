using System.Windows;
using System.Windows.Controls;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class CardsGridView : UserControl
{
    /// <summary>When true (Credit Card tab), shows the Next Invoice Due Date/Active columns for
    /// managing the card itself, alongside this month's statement. False (Summary tab) shows the
    /// statement columns only, unchanged from before the merge.</summary>
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
    }

    private void OnBankComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: BankDTO bank } comboBox)
        {
            return;
        }

        if (comboBox.DataContext is not CreditCardManagementRow { Statement: { } statement } || DataContext is not MonthlyViewModel viewModel)
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

        if (DataContext is not MonthlyViewModel viewModel)
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

        if (DataContext is not MonthlyViewModel viewModel)
        {
            return;
        }

        _ = viewModel.UpdateCreditCardAsync(row.CreditCard, row.CreditCard.NextInvoiceDueDate, checkBox.IsChecked == true);
    }
}
