using Financial.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static FinanacialTools.BrokerTotal;

namespace FinanacialTools.Components
{
    /// <summary>
    /// Interaction logic for AssetInfo.xaml
    /// </summary>
    public partial class AssetInfo : UserControl
    {
        public event EventHandler ButtonClicked;


        public AssetInfo()
        {
            InitializeComponent();
        }

        internal void LoadAssets(BrokerInfoDTO brokerInfo)
        {
            List<PortifolioOption> options = new List<PortifolioOption>();
            foreach (var portifolio in brokerInfo.PortifiliosActive)
            {
                foreach (var asset in portifolio.Assets)
                {
                    options.Add(new PortifolioOption { Group = "Active", PortifolioName = portifolio.Name, AssetName = asset });
                }
            }
            foreach (var portifolio in brokerInfo.PortifiliosInactive)
            {
                foreach (var asset in portifolio.Assets)
                {
                    options.Add(new PortifolioOption { Group = "Inactive", PortifolioName = portifolio.Name, AssetName = asset });
                }
            }
            var groupedOptions = new ListCollectionView(options);
            groupedOptions.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            txtPortifolioAssets.ItemsSource = groupedOptions;
        }

        private void btnLoadAsset_Click(object sender, RoutedEventArgs e)
        {
            ButtonClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
