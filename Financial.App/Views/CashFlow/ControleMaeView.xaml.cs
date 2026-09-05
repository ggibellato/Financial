using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class ControleMaeView : UserControl
{
    public ControleMaeView(ControleMaeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Loaded += async (_, _) => await viewModel.RefreshEntriesAsync();
    }
}
