using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Rules;
using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.Options;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Financial.Presentation.App.Views;

public partial class DividendCheckView : UserControl
{
    private readonly IDividendService _dividendService;

    private const string BvmfExchange = "BVMF";

    public DividendCheckView(IDividendService dividendService)
    {
        _dividendService = dividendService ?? throw new ArgumentNullException(nameof(dividendService));

        InitializeComponent();

        var groupedOptions = new ListCollectionView(new List<WatchlistItem>(WatchlistOptions.DefaultDividendWatchlist));
        groupedOptions.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        txtTicker.ItemsSource = groupedOptions;
    }

    private void btnCheck_Click(object sender, RoutedEventArgs e)
    {
        var ticker = txtTicker.Text.ToUpperInvariant();
        var request = new DividendLookupRequestDTO { Exchange = BvmfExchange, Ticker = ticker };

        var summary = _dividendService.GetDividendSummary(request);

        dividendDataGrid.ItemsSource = summary.History;
        dividendByYearDataGrid.ItemsSource = summary.YearTotals;

        lblName.Text = $"{summary.Ticker} - {summary.Name}";
        lblPrice.Text = $"Current price: {summary.CurrentPrice:N2}";
        lblAverageDividend.Text = $"Average Dividend: {summary.AverageDividendPerYear:F2} (last {DividendValuationRules.DividendYearsLookback} years)";
        lblPriceMax.Text = $"Price max buy: {summary.PriceMaxBuy:F2}   Discount {summary.DiscountPercent:F2}%";
        lblPriceMax.Foreground = summary.PriceMaxBuy > summary.CurrentPrice ? Brushes.Green : Brushes.Red;
    }

    private void DividendDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyType == typeof(DateTime))
        {
            if (e.Column is DataGridTextColumn dataGridColumn)
            {
                dataGridColumn.Binding.StringFormat = DateFormatHelper.GetPaddedShortDatePattern();
            }
        }

        ApplyValueColumnStyle(e, "Value");
    }

    private void DividendByYearDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        ApplyValueColumnStyle(e, "Total");
    }

    private static void ApplyValueColumnStyle(DataGridAutoGeneratingColumnEventArgs e, string propertyName)
    {
        if (!string.Equals(e.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
            return;

        if (e.Column is not DataGridTextColumn dataGridColumn)
            return;

        if (dataGridColumn.Binding is Binding binding)
        {
            binding.StringFormat = "N2";
        }
        else
        {
            dataGridColumn.Binding = new Binding(propertyName) { StringFormat = "N2" };
        }

        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
        dataGridColumn.ElementStyle = style;
    }
}
