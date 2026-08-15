using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class MonthlyView : UserControl
{
    public MonthlyView(MonthlyViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Loaded += async (_, _) => await viewModel.RefreshAsync();
    }
}
