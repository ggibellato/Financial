using Financial.Presentation.App.ViewModels.Admin;
using Financial.Presentation.App.ViewModels.Investment;

namespace Financial.Presentation.App.Services;

/// <summary>
/// Wraps the WPF-specific mechanics (MessageBox, modal Window) a ViewModel would otherwise call
/// directly, keeping ViewModels free of a compiled dependency on System.Windows types.
/// </summary>
public interface IDialogService
{
    bool Confirm(string message, string caption);
    void ShowWarning(string message, string caption);
    bool ShowMoveAssetDialog(MoveAssetDialogViewModel viewModel);
    bool ShowBrokerFormDialog(BrokerFormDialogViewModel viewModel);
    bool ShowBankFormDialog(BankFormDialogViewModel viewModel);
    bool ShowPortfolioFormDialog(PortfolioFormDialogViewModel viewModel);
    bool ShowAssetFormDialog(AssetFormDialogViewModel viewModel);
    bool ShowCategoryFormDialog(CategoryFormDialogViewModel viewModel);
}
