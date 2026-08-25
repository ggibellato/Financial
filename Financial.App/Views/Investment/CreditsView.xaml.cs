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

    private void OnCreditsPlotSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is IMainNavigationViewModel viewModel)
        {
            viewModel.AssetDetails.Credits.UpdatePlotWidth(e.NewSize.Width);
        }
    }
}
