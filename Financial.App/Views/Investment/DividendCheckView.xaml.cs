using Financial.Investment.Application.Configuration;
using Microsoft.Extensions.Options;
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

        var groupedOptions = new ListCollectionView(new List<WatchlistItemDTO>(watchlistOptions.Value.Items));
        groupedOptions.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        txtTicker.ItemsSource = groupedOptions;
    }
}
