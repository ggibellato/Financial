using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Financial.Presentation.App.ViewModels.Investment.Dashboard;

namespace Financial.Presentation.App.Views.Investment.Dashboard;

public partial class DataQualityWarningsView : UserControl
{
    public DataQualityWarningsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is DataQualityWarningsViewModel oldViewModel)
        {
            oldViewModel.CategoryExpanded -= OnCategoryExpanded;
        }

        if (e.NewValue is DataQualityWarningsViewModel newViewModel)
        {
            newViewModel.CategoryExpanded += OnCategoryExpanded;
        }
    }

    // A cross-panel link opens a category the user cannot see yet, so scrolling alone would leave a
    // keyboard or screen-reader user with no signal that anything happened - and the Expander it
    // opens is only realised once the layout pass that follows the expansion has run, hence the
    // deferral. Neither BringIntoView nor focus has a data-bindable XAML equivalent.
    private void OnCategoryExpanded(object? sender, DataQualityCategory category)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (DataContext is not DataQualityWarningsViewModel viewModel)
            {
                return;
            }

            var target = viewModel.Categories.FirstOrDefault(entry => entry.Category == category);
            if (target is null || CategoryList.ItemContainerGenerator.ContainerFromItem(target) is not FrameworkElement container)
            {
                return;
            }

            container.BringIntoView();

            if (!container.Focus())
            {
                container.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        });
    }
}
