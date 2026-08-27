using Financial.Presentation.App.ViewModels.Investment;
using System.Windows;

namespace Financial.Presentation.App.Views.Investment;

public partial class MoveAssetDialog : Window
{
    public MoveAssetDialog(MoveAssetDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DialogCloser.Attach(this, h => viewModel.CloseRequested += h, h => viewModel.CloseRequested -= h);
    }
}
