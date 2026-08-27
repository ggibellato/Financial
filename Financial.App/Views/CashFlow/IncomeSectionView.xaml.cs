using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class IncomeSectionView : UserControl
{
    public IncomeSectionView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // DataGridColumn isn't part of the visual tree, so its Header can't bind to the ambient
    // DataContext - it's assigned directly to the filter ViewModel instance here instead, and
    // renders through the FilterableColumnHeader implicit DataTemplate in App.xaml.
    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is IncomeWorkflowViewModel viewModel)
        {
            BankColumn.Header = viewModel.BankFilter;
        }
    }
}
