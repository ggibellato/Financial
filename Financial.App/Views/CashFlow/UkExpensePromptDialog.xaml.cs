using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.Presentation.App.Views.Investment;
using System.Windows;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class UkExpensePromptDialog : Window
{
    public UkExpensePromptDialog(UkExpensePromptDialogViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
        DialogCloser.Attach(this, h => viewModel.CloseRequested += h, h => viewModel.CloseRequested -= h);
    }
}
