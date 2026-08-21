using System.Windows.Controls;

namespace Financial.Presentation.App.Components
{
    /// <summary>
    /// Interaction logic for NavigationView.xaml. The four Investment tabs live in their own views
    /// under Views/Investment; the plot size-changed handlers moved with the markup that raises them.
    /// </summary>
    public partial class NavigationView : UserControl
    {
        public NavigationView()
        {
            InitializeComponent();
        }
    }
}
