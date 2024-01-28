using Financial.Model;
using FinancialModel.Application;
using FinancialToolSupport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;

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


        public BrokerTotal(Broker broker, IRepository repository)
        {
            _broker = broker;
            _repository = repository;
            InitializeComponent();
            lblCurrency.Content = broker.Currency;
        }

        private void btnLoad_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var totalBought = _repository.GetTotalBoughtByBroker(_broker.Name);
            var totalSold = _repository.GetTotalSoldByBroker(_broker.Name);
            var totalCredits = _repository.GetTotalCreditsByBroker(_broker.Name);
            lblTotalBought.Content = totalBought.FormatCurrency(_broker.Currency);
            lblTotalSold.Content = totalSold.FormatCurrency(_broker.Currency);
            lblTotalCredits.Content = totalCredits.FormatCurrency(_broker.Currency);
            lblCurrentInvested.Content = (totalBought - totalSold + totalCredits).FormatCurrency(_broker.Currency);
        }
    }
}
