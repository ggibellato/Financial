using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Admin;

namespace Financial.Presentation.App.Views.Admin;

public partial class CategoriesView : UserControl
{
    public CategoriesView(CategoriesViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
