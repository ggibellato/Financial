using System.Windows;
using System.Windows.Controls;
using Financial.Presentation.App.Behaviors;
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

    private void OnCategoryExpanded(object? sender, DataQualityCategory category)
    {
        if (DataContext is not DataQualityWarningsViewModel viewModel)
        {
            return;
        }

        var target = viewModel.Categories.FirstOrDefault(entry => entry.Category == category);
        if (target is null)
        {
            return;
        }

        ItemFocusHelper.FocusAndScrollIntoView(Dispatcher, CategoryList, target);
    }
}
