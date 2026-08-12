using System.Windows;
using System.Windows.Controls;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class CreditCardsGridView : UserControl
{
    public CreditCardsGridView()
    {
        InitializeComponent();
    }

    private void OnDueDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DatePicker { DataContext: CreditCardDTO card } picker)
        {
            return;
        }

        if (DataContext is not MonthlyViewModel viewModel)
        {
            return;
        }

        var newDueDate = picker.SelectedDate.HasValue ? DateOnly.FromDateTime(picker.SelectedDate.Value) : (DateOnly?)null;
        _ = viewModel.UpdateCreditCardAsync(card, newDueDate, card.IsActive);
    }

    private void OnActiveChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { DataContext: CreditCardDTO card } checkBox)
        {
            return;
        }

        if (DataContext is not MonthlyViewModel viewModel)
        {
            return;
        }

        _ = viewModel.UpdateCreditCardAsync(card, card.NextInvoiceDueDate, checkBox.IsChecked == true);
    }
}
