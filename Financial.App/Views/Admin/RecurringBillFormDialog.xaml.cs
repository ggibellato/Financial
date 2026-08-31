using Financial.Presentation.App.ViewModels.Admin;
using Financial.Presentation.App.Views.Investment;
using System.Windows;

namespace Financial.Presentation.App.Views.Admin;

public partial class RecurringBillFormDialog : Window
{
    public RecurringBillFormDialog(RecurringBillFormDialogViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
        DialogCloser.Attach(this, h => viewModel.CloseRequested += h, h => viewModel.CloseRequested -= h);
    }
}
