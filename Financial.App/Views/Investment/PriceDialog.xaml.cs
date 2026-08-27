using System.Windows;

namespace Financial.Presentation.App.Views.Investment;

public partial class PriceDialog : Window
{
    public PriceDialog(PriceDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DialogCloser.Attach(this, h => viewModel.CloseRequested += h, h => viewModel.CloseRequested -= h);
    }
}
