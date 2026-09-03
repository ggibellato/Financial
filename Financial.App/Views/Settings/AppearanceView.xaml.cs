using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Settings;

namespace Financial.Presentation.App.Views.Settings;

public partial class AppearanceView : UserControl
{
    public AppearanceView(ColourModeViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
