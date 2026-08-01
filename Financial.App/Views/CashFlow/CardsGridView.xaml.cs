using System.Windows.Controls;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class CardsGridView : UserControl
{
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

        if (comboBox.DataContext is not CardStatementDTO statement || DataContext is not MonthlyViewModel viewModel)
        {
            return;
        }

        viewModel.SetMarkPaidSource(statement.Id, bank.Name);
    }
}
