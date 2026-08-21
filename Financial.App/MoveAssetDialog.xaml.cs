using Financial.Presentation.App.ViewModels.Investment;
using System.Windows;

namespace Financial.Presentation.App;

public partial class MoveAssetDialog : Window
{
    public MoveAssetDialog(MoveAssetDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool? dialogResult)
    {
        if (sender is MoveAssetDialogViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }

        DialogResult = dialogResult;
        Close();
    }
}
