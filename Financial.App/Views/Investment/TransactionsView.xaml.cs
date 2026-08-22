using Financial.Presentation.App.ViewModels.Investment;
using System.Windows;
using System.Windows.Controls;

namespace Financial.Presentation.App.Views.Investment;

public partial class TransactionsView : UserControl
{
    public TransactionsView()
    {
        InitializeComponent();
    }

    private void OnTransactionsPlotSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is IMainNavigationViewModel viewModel)
        {
            viewModel.AssetDetails.UpdateTransactionsPlotWidth(e.NewSize.Width);
        }
    }
}
