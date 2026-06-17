using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.Options;
using Financial.Presentation.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Financial.Presentation.App.Views;

public partial class DividendCheckView : UserControl
{
    public DividendCheckView(DividendCheckViewModel viewModel, WatchlistOptions watchlistOptions)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(watchlistOptions);

        InitializeComponent();
        DataContext = viewModel;

        var groupedOptions = new ListCollectionView(new List<WatchlistItem>(watchlistOptions.Items));
        groupedOptions.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        txtTicker.ItemsSource = groupedOptions;
    }

    private void DividendDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyType == typeof(DateTime) && e.Column is DataGridTextColumn dateColumn)
            dateColumn.Binding.StringFormat = DateFormatHelper.GetPaddedShortDatePattern();

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

        if (e.Column is not DataGridTextColumn col)
            return;

        if (col.Binding is System.Windows.Data.Binding binding)
            binding.StringFormat = "N2";
        else
            col.Binding = new System.Windows.Data.Binding(propertyName) { StringFormat = "N2" };

        var style = new System.Windows.Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Black));
        col.ElementStyle = style;
    }
}
