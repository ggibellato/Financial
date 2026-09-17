using System.Windows;
using System.Windows.Controls;
using Financial.Presentation.App.ViewModels.Investment.Dashboard;

namespace Financial.Presentation.App.Views.Investment.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is DashboardViewModel oldViewModel)
        {
            oldViewModel.RecoveredFromError -= OnRecoveredFromError;
        }

        if (e.NewValue is DashboardViewModel newViewModel)
        {
            newViewModel.RecoveredFromError += OnRecoveredFromError;
        }
    }

    // Retrying swaps the whole page's content back to the panels, so the button the user just
    // pressed is gone: move focus to the page heading so keyboard users don't lose their place.
    private void OnRecoveredFromError(object? sender, EventArgs e) => DashboardHeading.Focus();
}
