﻿using Financial.Model;
using FinancialModel.Application;
using FinancialToolSupport;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        }

        private void btnLoad_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var brokerInfo = _repository.GetBrokerInfo(_broker.Name);

            lblTotalBought.Content = brokerInfo.TotalBought.FormatCurrency(_broker.Currency);
            lblTotalSold.Content = brokerInfo.TotalSold.FormatCurrency(_broker.Currency);
            lblTotalCredits.Content = brokerInfo.TotalCredits.FormatCurrency(_broker.Currency);

            lblTotalActiveBought.Content = brokerInfo.TotalBoughtActive.FormatCurrency(_broker.Currency);
            lblTotalActiveSold.Content = brokerInfo.TotalSoldActive.FormatCurrency(_broker.Currency);
            lblTotalActiveCredits.Content = brokerInfo.TotalCreditsActive.FormatCurrency(_broker.Currency);

            List<PortifolioOption> options = new List<PortifolioOption>();
            foreach ( var portifolio in brokerInfo.PortifiliosActive)
            {
                foreach(var asset in portifolio.Assets)
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

        private void btnLoadAsset_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(txtPortifolioAssets.SelectedItem is not null)
            {
                var selected = (PortifolioOption)txtPortifolioAssets.SelectedItem;
                var assetInfo = _repository.GetAssetInfo(_broker.Name, selected.PortifolioName, selected.AssetName);

                lblQuantity.Content = $"{assetInfo.Quantity:n6}";
                lblAssetTotalBought.Content = assetInfo.TotalBought.FormatCurrency(_broker.Currency);
                lblAssetTotalSold.Content = assetInfo.TotalSold.FormatCurrency(_broker.Currency);
                lblAssetTotalCredits.Content = assetInfo.Credits.Total.FormatCurrency(_broker.Currency);
                var x = assetInfo.Credits.CreditsByMonth.Sum(cm => cm.Value);
            }
        }
    }
}
