using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Settings;

namespace Financial.Presentation.App.Views.Settings;

public partial class SettingsIntegrationsView : UserControl
{
    public SettingsIntegrationsView(SettingsIntegrationsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
