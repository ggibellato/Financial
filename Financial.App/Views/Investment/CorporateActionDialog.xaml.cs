using System.Windows;

namespace Financial.Presentation.App.Views.Investment;

public partial class CorporateActionDialog : Window
{
    public CorporateActionDialog(CorporateActionFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DialogCloser.Attach(this, h => viewModel.CloseRequested += h, h => viewModel.CloseRequested -= h);
    }
}
