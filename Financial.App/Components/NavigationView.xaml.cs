using System.Windows;
using System.Windows.Controls;
using Financial.Presentation.App.ViewModels;

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
            if (DataContext is MainNavigationViewModel viewModel)
            {
                viewModel.AssetDetails.UpdateCreditsPlotWidth(e.NewSize.Width);
            }
        }
    }
}



