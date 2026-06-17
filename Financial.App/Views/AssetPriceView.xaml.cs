using Financial.Presentation.App.ViewModels;
using System.Windows.Controls;

namespace Financial.Presentation.App.Views;

public partial class AssetPriceView : UserControl
{
    public AssetPriceView(AssetPriceFetchViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }
}
