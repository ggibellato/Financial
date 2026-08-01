using System.Windows.Controls;

namespace Financial.Presentation.App.Views.Investment;

public partial class AssetPriceView : UserControl
{
    public AssetPriceView(AssetPriceFetchViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }
}
