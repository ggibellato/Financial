using System.Windows.Controls;

namespace Financial.Presentation.App.Views.Admin;

public partial class AdminEntityPlaceholderView : UserControl
{
    public AdminEntityPlaceholderView(AdminEntityPlaceholderViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
