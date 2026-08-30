using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Admin;

namespace Financial.Presentation.App.Views.Admin;

public partial class BrokersView : UserControl
{
    public BrokersView(BrokersViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
