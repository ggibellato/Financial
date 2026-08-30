using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Admin;

namespace Financial.Presentation.App.Views.Admin;

public partial class AssetsView : UserControl
{
    public AssetsView(AssetsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
