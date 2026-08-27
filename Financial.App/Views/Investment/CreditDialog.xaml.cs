using System.Windows;

namespace Financial.Presentation.App.Views.Investment;

public partial class CreditDialog : Window
{
    public CreditDialog(CreditDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool? dialogResult)
    {
        if (sender is CreditDialogViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }

        DialogResult = dialogResult;
        Close();
    }
}
