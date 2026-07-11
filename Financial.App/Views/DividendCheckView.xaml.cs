using Financial.Application.Configuration;
using Financial.Presentation.App.Helpers;
using Microsoft.Extensions.Options;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Financial.Presentation.App.Views;

public partial class DividendCheckView : UserControl
{
    public DividendCheckView(DividendCheckViewModel viewModel, IOptions<WatchlistOptions> watchlistOptions)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(watchlistOptions);

        InitializeComponent();
        DataContext = viewModel;

        var groupedOptions = new ListCollectionView(new List<WatchlistItem>(watchlistOptions.Value.Items));
        groupedOptions.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        txtTicker.ItemsSource = groupedOptions;
    }

    private void DividendDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyType == typeof(DateTime) && e.Column is DataGridTextColumn dateColumn)
            dateColumn.Binding.StringFormat = DateFormatHelper.GetPaddedShortDatePattern();

        if (!ApplyValueColumnStyle(e, "Value"))
            ApplyPlainColumnStyle(e);
    }

    private void DividendByYearDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (!ApplyValueColumnStyle(e, "Total"))
            ApplyPlainColumnStyle(e);
    }

    private static bool ApplyValueColumnStyle(DataGridAutoGeneratingColumnEventArgs e, string propertyName)
    {
        if (!string.Equals(e.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (e.Column is not DataGridTextColumn col)
            return false;

        if (col.Binding is System.Windows.Data.Binding binding)
            binding.StringFormat = "N2";
        else
            col.Binding = new System.Windows.Data.Binding(propertyName) { StringFormat = "N2" };

        var style = new System.Windows.Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Black));
        style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        col.ElementStyle = style;
        return true;
    }

    private static void ApplyPlainColumnStyle(DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column is not DataGridTextColumn col)
            return;

        var style = new System.Windows.Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        col.ElementStyle = style;
    }
}
