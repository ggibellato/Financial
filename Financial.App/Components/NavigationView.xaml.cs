using System.Windows;
using System.Windows.Controls;

namespace Financial.Presentation.App.Components
{
    /// <summary>
    /// Interaction logic for NavigationView.xaml
    /// </summary>
    public partial class NavigationView : UserControl
    {
        public NavigationView()
        {
            InitializeComponent();
        }

        private void OnCreditsPlotSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is IMainNavigationViewModel viewModel)
            {
                viewModel.AssetDetails.UpdateCreditsPlotWidth(e.NewSize.Width);
            }
        }

        private void OnTransactionsPlotSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is IMainNavigationViewModel viewModel)
            {
                viewModel.AssetDetails.UpdateTransactionsPlotWidth(e.NewSize.Width);
            }
        }
    }
}



