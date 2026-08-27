using System.Windows;

namespace Financial.Presentation.App.Views.Investment;

public partial class PriceDialog : Window
{
    public PriceDialog(PriceDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool? dialogResult)
    {
        if (sender is PriceDialogViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }

        DialogResult = dialogResult;
        Close();
    }
}
