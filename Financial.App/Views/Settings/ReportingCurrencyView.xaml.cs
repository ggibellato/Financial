using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Settings;

namespace Financial.Presentation.App.Views.Settings;

public partial class ReportingCurrencyView : UserControl
{
    public ReportingCurrencyView(ReportingCurrencyViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
