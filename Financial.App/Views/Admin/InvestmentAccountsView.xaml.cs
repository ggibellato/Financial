using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Admin;

namespace Financial.Presentation.App.Views.Admin;

public partial class InvestmentAccountsView : UserControl
{
    public InvestmentAccountsView(InvestmentAccountsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
