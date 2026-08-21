using Financial.Presentation.App.ViewModels.Investment;
using System.Windows;
using System.Windows.Controls;

namespace Financial.Presentation.App.Views.Investment;

public partial class CreditsView : UserControl
{
    public CreditsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Moved here with the markup that raises it. DataContext is inherited from the hosting
    /// TabItem, so the view model this reaches is the same one NavigationView saw.
    /// </summary>
    private void OnCreditsPlotSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is IMainNavigationViewModel viewModel)
        {
            viewModel.AssetDetails.UpdateCreditsPlotWidth(e.NewSize.Width);
        }
    }
}
