using Financial.Investment.Application.Configuration;
using Financial.Presentation.App.Helpers;
using Microsoft.Extensions.Options;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Financial.Presentation.App.Views.Investment;

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

        var style = new System.Windows.Style(typeof(TextBlock), FindSharedStyle("NumericColumnTextStyle"));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Black));
        col.ElementStyle = style;
        return true;
    }

    private static void ApplyPlainColumnStyle(DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column is not DataGridTextColumn col)
            return;

        col.ElementStyle = new System.Windows.Style(typeof(TextBlock), FindSharedStyle("PlainColumnTextStyle"));
    }

    /// <summary>
    /// Auto-generated columns build their <c>ElementStyle</c> in code, so it must be based on the shared
    /// keyed style the same way a static column's markup would via <c>BasedOn</c> - otherwise a future
    /// change to the shared style (e.g. padding) silently stops applying here.
    /// </summary>
    private static System.Windows.Style FindSharedStyle(string resourceKey) =>
        (System.Windows.Style)Application.Current.FindResource(resourceKey);
}
