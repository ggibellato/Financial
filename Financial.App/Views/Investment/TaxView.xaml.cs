using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Investment;

namespace Financial.Presentation.App.Views.Investment;

public partial class TaxView : UserControl
{
    public TaxView(TaxWorkbookViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
