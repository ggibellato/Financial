using Microsoft.VisualBasic.FileIO;
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
using WebPageParser;

namespace SharesDividendCheck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //< ComboBoxItem Content = "KLBN4" />
            //< ComboBoxItem Content = "TASA4" />
            //< ComboBoxItem Content = "TAEE3" />
            //< ComboBoxItem Content = "UNIP6" />
            //< ComboBoxItem Content = "CMIG4" />
            //< ComboBoxItem Content = "TRPL4" />
            //< ComboBoxItem Content = "BBAS3" />
            //< ComboBoxItem Content = "CSAN3" />


            List < Option> options = new List<Option>
            {
                new Option { Group = "Ja possuidas", Name = "KLBN4" },
                new Option { Group = "Ja possuidas", Name = "TASA4" },
                new Option { Group = "Ja possuidas", Name = "TAEE3" },
                new Option { Group = "Outras Barse", Name = "UNIP6" },
                new Option { Group = "Outras Barse", Name = "CMIG4" },
                new Option { Group = "Outras Barse", Name = "TRPL4" },
                new Option { Group = "Outras Barse", Name = "BBAS3" },
                new Option { Group = "Outras", Name = "CSAN3" },
                // Add more options and groups as needed
            };

            var groupedOptions = new ListCollectionView(options);
            groupedOptions.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            txtTicker.ItemsSource = groupedOptions;
        }

        public class Option
        {
            public string Group { get; set; }
            public string Name { get; set; }
        }

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            var value = GoogleFinance.GetFinancialInfo(txtTicker.Text);
            var dividends = DadosMercadoDividend.GetDividendInfo(txtTicker.Text);
            dividendDataGrid.ItemsSource = dividends;

            var dividendsByYear = dividends
                .GroupBy(d => d.Date.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Total = g.Sum(gi => gi.Value)
                })
                .OrderByDescending(dy => dy.Year);
            dividendByYearDataGrid.ItemsSource = dividendsByYear;

            var averageDividend = dividendsByYear.Where(dy => dy.Year != DateTime.Now.Year).Take(5).Average(dy => dy.Total);
            var priceMax = averageDividend / (decimal)0.06;
            var priceDiscountPer = (1 - value.Price / priceMax)*100;

            lblName.Content = $"{value.Ticker} - {value.Name}";
            lblPrice.Content = $"Current price: {value.Price}";
            lblAverageDividend.Content = $"Average Dividend: {averageDividend.ToString("F2")} (last 5 years)";
            lblPriceMax.Content = $"Price max buy: {priceMax.ToString("F2")}   Discount {priceDiscountPer.ToString("F2")}%";
            lblPriceMax.Foreground = priceMax > value.Price ? Brushes.Green : Brushes.Red;
        }

        private void DividendDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTime))
            {
                var dataGridColumn = e.Column as DataGridTextColumn;
                if (dataGridColumn != null)
                {
                    dataGridColumn.Binding.StringFormat = "dd/MM/yyyy";
                }
            }
        }
    }
}
