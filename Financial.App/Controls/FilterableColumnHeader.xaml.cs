using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Financial.Presentation.App.ViewModels;

namespace Financial.Presentation.App.Controls;

/// <summary>
/// Header content for a filterable DataGridColumn: a label, a filter icon toggle, and a popup
/// checklist. Assign a DataGridColumn's Header to a ColumnFilterViewModelBase instance and it
/// renders through this control automatically, via the implicit DataTemplate in App.xaml.
///
/// The search box narrows which checklist items are VISIBLE only - it never touches IsChecked,
/// matching F03's Web behavior. That's a pure view concern, so it's implemented here via a
/// CollectionViewSource rather than in the ViewModel.
/// </summary>
public partial class FilterableColumnHeader : UserControl
{
    private readonly CollectionViewSource _optionsView = new();

    public FilterableColumnHeader()
    {
        InitializeComponent();
        OptionsItemsControl.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = _optionsView, Path = new PropertyPath("View") });
        _optionsView.Filter += OnFilterOptions;
        DataContextChanged += OnDataContextChanged;
        SearchBox.TextChanged += OnSearchTextChanged;
    }

    private string _searchText = string.Empty;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not ColumnFilterViewModelBase viewModel)
        {
            return;
        }

        _optionsView.Source = viewModel.Options;
        _searchText = string.Empty;
        SearchBox.Text = string.Empty;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        _optionsView.View?.Refresh();
    }

    private void OnFilterOptions(object sender, FilterEventArgs e)
    {
        e.Accepted = string.IsNullOrEmpty(_searchText)
            || (e.Item is FilterValueOption option && option.Value.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
    }
}
