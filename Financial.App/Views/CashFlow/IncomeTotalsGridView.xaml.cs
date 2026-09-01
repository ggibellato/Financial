using System.Windows;
using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class IncomeTotalsGridView : UserControl
{
    public IncomeTotalsGridView()
    {
        InitializeComponent();
    }

    private void OnCarryForwardIncludedChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || DataContext is not MonthlyViewModel viewModel)
        {
            return;
        }

        _ = viewModel.UpdateTitheCarryForwardAsync(checkBox.IsChecked == true);
    }
}
