using Financial.Presentation.App.ViewModels.Admin;
using Financial.Presentation.App.ViewModels.Investment;
using Financial.Presentation.App.Views.Admin;
using Financial.Presentation.App.Views.Investment;

namespace Financial.Presentation.App.Services;

public sealed class DialogService : IDialogService
{
    public bool Confirm(string message, string caption) =>
        System.Windows.MessageBox.Show(message, caption, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
            == System.Windows.MessageBoxResult.Yes;

    public void ShowWarning(string message, string caption) =>
        System.Windows.MessageBox.Show(message, caption, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

    public bool ShowMoveAssetDialog(MoveAssetDialogViewModel viewModel) =>
        new MoveAssetDialog(viewModel) { Owner = System.Windows.Application.Current?.MainWindow }.ShowDialog() == true;

    public bool ShowBrokerFormDialog(BrokerFormDialogViewModel viewModel) =>
        new BrokerFormDialog(viewModel) { Owner = System.Windows.Application.Current?.MainWindow }.ShowDialog() == true;

    public bool ShowPortfolioFormDialog(PortfolioFormDialogViewModel viewModel) =>
        new PortfolioFormDialog(viewModel) { Owner = System.Windows.Application.Current?.MainWindow }.ShowDialog() == true;
}
