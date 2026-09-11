using System.Windows.Controls;

namespace Financial.Presentation.App.Views.Investment;

public partial class AssetPriceView : UserControl
{
    public AssetPriceFetchViewModel ViewModel { get; }

    public AssetPriceView(AssetPriceFetchViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }
}
