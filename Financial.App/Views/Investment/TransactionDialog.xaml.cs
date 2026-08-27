using System.Windows;

namespace Financial.Presentation.App.Views.Investment;

public partial class TransactionDialog : Window
{
    public TransactionDialog(TransactionDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool? dialogResult)
    {
        if (sender is TransactionDialogViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }

        DialogResult = dialogResult;
        Close();
    }
}
