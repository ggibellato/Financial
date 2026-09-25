using System.Windows;
using System.Windows.Controls;
using Financial.Presentation.App.Behaviors;
using Financial.Presentation.App.ViewModels.Investment;

namespace Financial.Presentation.App.Views.Investment;

public partial class CorporateActionsView : UserControl
{
    public CorporateActionsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is IMainNavigationViewModel oldNav)
        {
            oldNav.AssetDetails.CorporateActions.FocusRequested -= OnFocusRequested;
        }

        if (e.NewValue is IMainNavigationViewModel newNav)
        {
            newNav.AssetDetails.CorporateActions.FocusRequested += OnFocusRequested;
        }
    }

    private void OnFocusRequested(object? sender, CorporateActionRowViewModel target) =>
        ItemFocusHelper.FocusAndScrollIntoView(Dispatcher, HistoryGrid, target);
}
