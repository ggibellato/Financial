using FinanacialTools.Components;
using Financial.Model;
using FinancialModel.Application;
using FinancialToolSupport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using static SharesDividendCheck.MainWindow;

namespace FinanacialTools
{
    /// <summary>
    /// Interaction logic for BrokerTotal.xaml
    /// </summary>
    public partial class BrokerTotal : UserControl
    {
        private readonly Broker _broker;
        private readonly IRepository _repository;
        public ObservableCollection<KeyValuePair<DateOnly, decimal>> _chartData { get; set; } = new ObservableCollection<KeyValuePair<DateOnly, decimal>>();

        private Dictionary<string, string> _currencyRegionInfo = new Dictionary<string, string>()
        {
            {"GBP", "UK"},
            {"BRL", "BR"}
        };

        public class PortifolioOption
        {
            public required string Group { get; set; }
            public required string PortifolioName { get; set; }
            public required string AssetName { get; set; }
        }


        public BrokerTotal(Broker broker, IRepository repository)
        {
            _broker = broker;
            _repository = repository;
            InitializeComponent();
            lblCurrency.Content = broker.Currency;

            ((ColumnSeries)AssetCredits.Series[0]).ItemsSource = _chartData;
        }

        private void btnLoad_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var brokerInfo = _repository.GetBrokerInfo(_broker.Name);

            Total.lblTotalBought.Content = brokerInfo.TotalBought.FormatCurrency(_broker.Currency);
            Total.lblTotalSold.Content = brokerInfo.TotalSold.FormatCurrency(_broker.Currency);
            Total.lblTotalCredits.Content = brokerInfo.TotalCredits.Total.FormatCurrency(_broker.Currency);

            ActiveTotal.lblTotalBought.Content = brokerInfo.TotalBoughtActive.FormatCurrency(_broker.Currency);
            ActiveTotal.lblTotalSold.Content = brokerInfo.TotalSoldActive.FormatCurrency(_broker.Currency);
            ActiveTotal.lblTotalCredits.Content = brokerInfo.TotalCreditsActive.Total.FormatCurrency(_broker.Currency);

            AssetInfo.LoadAssets(brokerInfo);
        }

        private void AssetInfo_ButtonClicked(object sender, System.EventArgs e)
        {
            if (AssetInfo.txtPortifolioAssets.SelectedItem is not null)
            {
                var selected = (PortifolioOption)AssetInfo.txtPortifolioAssets.SelectedItem;
                var assetInfo = _repository.GetAssetInfo(_broker.Name, selected.PortifolioName, selected.AssetName);

                AssetInfo.lblQuantity.Content = $"{assetInfo.Quantity:n6}";
                AssetInfo.AssetTotal.lblTotalBought.Content = assetInfo.TotalBought.FormatCurrency(_broker.Currency);
                AssetInfo.AssetTotal.lblTotalSold.Content = assetInfo.TotalSold.FormatCurrency(_broker.Currency);
                AssetInfo.AssetTotal.lblTotalCredits.Content = assetInfo.Credits.Total.FormatCurrency(_broker.Currency);

                _chartData.Clear();
                foreach (var item in assetInfo.Credits.CreditsByMonth)
                {
                    _chartData.Add(new KeyValuePair<DateOnly, decimal>(item.Key, item.Value));
                }
            }
        }
    }
}
