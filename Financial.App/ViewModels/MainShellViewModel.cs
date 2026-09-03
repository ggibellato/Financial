using Financial.Presentation.App.Navigation;
using Financial.Presentation.App.ViewModels.Settings;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Owns the shell's collapsed/expanded state and the currently selected destination view.
/// Persistence is delegated to an injected callback so this ViewModel has no dependency on
/// <see cref="Properties.Settings"/> and stays unit-testable.
/// </summary>
public class MainShellViewModel : ViewModelBase
{
    private readonly IReadOnlyDictionary<string, object> _viewsByKey;
    private readonly Action<bool> _persistCollapsed;
    private bool _isCollapsed;
    private string? _selectedChildId;
    private object? _selectedContent;

    public MainShellViewModel(
        bool initialCollapsed,
        Action<bool> persistCollapsed,
        IReadOnlyDictionary<string, object> viewsByKey,
        SyncStatusViewModel syncStatusViewModel,
        PaymentDueBannerViewModel paymentDueBannerViewModel,
        ColourModeViewModel colourModeViewModel)
    {
        _persistCollapsed = persistCollapsed ?? throw new ArgumentNullException(nameof(persistCollapsed));
        _viewsByKey = viewsByKey ?? throw new ArgumentNullException(nameof(viewsByKey));
        SyncStatus = syncStatusViewModel ?? throw new ArgumentNullException(nameof(syncStatusViewModel));
        PaymentsDue = paymentDueBannerViewModel ?? throw new ArgumentNullException(nameof(paymentDueBannerViewModel));
        ColourMode = colourModeViewModel ?? throw new ArgumentNullException(nameof(colourModeViewModel));
        _isCollapsed = initialCollapsed;

        ToggleCollapsedCommand = new RelayCommand(ToggleCollapsed);
        SelectItemCommand = new RelayCommand<string>(SelectItem);

        var defaultChild = NavTree.Categories[0].Children[0];
        SelectItem(defaultChild.ViewKey);
    }

    public IReadOnlyList<NavCategory> Categories => NavTree.Categories;

    public SyncStatusViewModel SyncStatus { get; }

    public PaymentDueBannerViewModel PaymentsDue { get; }

    public ColourModeViewModel ColourMode { get; }

    public bool IsCollapsed
    {
        get => _isCollapsed;
        private set => SetProperty(ref _isCollapsed, value);
    }

    public string? SelectedChildId
    {
        get => _selectedChildId;
        private set
        {
            if (SetProperty(ref _selectedChildId, value))
            {
                OnPropertyChanged(nameof(BreadcrumbText));
            }
        }
    }

    public string BreadcrumbText
    {
        get
        {
            foreach (var category in Categories)
            {
                var child = category.Children.FirstOrDefault(c => c.ViewKey == _selectedChildId);
                if (child != null)
                {
                    return $"{category.Label} › {child.Label}";
                }

                foreach (var group in category.Groups ?? [])
                {
                    var groupedChild = group.Children.FirstOrDefault(c => c.ViewKey == _selectedChildId);
                    if (groupedChild != null)
                    {
                        return $"{category.Label} › {group.Label} › {groupedChild.Label}";
                    }
                }
            }

            return "—";
        }
    }

    public object? SelectedContent
    {
        get => _selectedContent;
        private set => SetProperty(ref _selectedContent, value);
    }

    public RelayCommand ToggleCollapsedCommand { get; }

    public RelayCommand<string> SelectItemCommand { get; }

    private void ToggleCollapsed()
    {
        IsCollapsed = !IsCollapsed;
        _persistCollapsed(IsCollapsed);
    }

    private void SelectItem(string? viewKey)
    {
        if (viewKey is null || !_viewsByKey.TryGetValue(viewKey, out var view))
        {
            return;
        }

        SelectedChildId = viewKey;
        SelectedContent = view;
    }
}
