using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class InvestmentSnapshotsView : UserControl
{
    public InvestmentSnapshotsView(InvestmentSnapshotsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
