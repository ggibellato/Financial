using Financial.Application.DTO;
using Financial.Model;
using FinancialToolSupport;
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

        internal void LoadData(AssetInfoDTO assetInfo, string currency)
        {
            lblQuantity.Content = $"{assetInfo.Quantity:n6}";
            lblAveragePrice.Content = assetInfo.AvaragePrice.FormatCurrency(currency);
            lblCurrentPrice.Content = assetInfo.CurrentValue.FormatCurrency(currency);
            lblResult.Content = String.Format("{0:P2}", (assetInfo.CurrentValue / assetInfo.AvaragePrice)-1);
            AssetTotal.lblTotalBought.Content = assetInfo.TotalBought.FormatCurrency(currency);
            AssetTotal.lblTotalSold.Content = assetInfo.TotalSold.FormatCurrency(currency);
            AssetTotal.lblTotalCredits.Content = assetInfo.Credits.Total.FormatCurrency(currency);
        }
    }
}
