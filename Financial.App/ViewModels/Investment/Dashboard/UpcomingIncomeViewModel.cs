using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class UpcomingIncomeViewModel : ViewModelBase
{
    private readonly IUpcomingIncomeService _incomeService;
    private readonly ILogger<UpcomingIncomeViewModel> _logger;

    private IReadOnlyList<UpcomingIncomeDTO> _allEntries = [];
    private UpcomingIncomeWindow _selectedWindow = UpcomingIncomeWindow.Days90;
    private bool _isLoading;
    private string? _errorMessage;
    private int _requestId;

    public UpcomingIncomeViewModel(
        IUpcomingIncomeService incomeService,
        ILogger<UpcomingIncomeViewModel> logger)
    {
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        SelectWindowCommand = new RelayCommand(SelectWindow);

        WindowOptions.Add(new SelectableOptionViewModel<UpcomingIncomeWindow>("30 days", UpcomingIncomeWindow.Days30));
        WindowOptions.Add(new SelectableOptionViewModel<UpcomingIncomeWindow>("90 days", UpcomingIncomeWindow.Days90));
        WindowOptions.Add(new SelectableOptionViewModel<UpcomingIncomeWindow>("180 days", UpcomingIncomeWindow.Days180));
        UpdateWindowSelection();
    }

    public RelayCommand RefreshCommand { get; }

    public RelayCommand SelectWindowCommand { get; }

    public ObservableCollection<SelectableOptionViewModel<UpcomingIncomeWindow>> WindowOptions { get; } = [];

    public ObservableCollection<UpcomingIncomeDTO> Entries { get; } = [];

    public UpcomingIncomeWindow SelectedWindow
    {
        get => _selectedWindow;
        set
        {
            if (SetProperty(ref _selectedWindow, value))
            {
                UpdateWindowSelection();
                OnPropertyChanged(nameof(EmptyStateText));
                ApplyWindowFilter();
            }
        }
    }

    public int SelectedWindowDays => (int)SelectedWindow;

    public string EmptyStateText => $"No upcoming payments detected in the next {SelectedWindowDays} days";

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                NotifyContentStateChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                NotifyContentStateChanged();
            }
        }
    }

    public bool HasError => ErrorMessage != null;

    public bool ShowContent => !IsLoading && !HasError;

    public bool IsEmpty => Entries.Count == 0;

    public bool ShowEmptyState => ShowContent && IsEmpty;

    public bool ShowEntries => ShowContent && !IsEmpty;

    public Task LoadAsync() => ExecuteRefreshAsync(
        () => ++_requestId,
        id => id == _requestId,
        loading => IsLoading = loading,
        error => ErrorMessage = error,
        isCurrent =>
        {
            var entries = _incomeService.GetUpcomingIncome();

            if (isCurrent())
            {
                _allEntries = entries;
                ApplyWindowFilter();
            }

            return Task.CompletedTask;
        },
        ex => _logger.LogError("Upcoming income refresh failed with {ErrorType}", ex.GetType().Name));

    private void SelectWindow(object? parameter)
    {
        if (parameter is SelectableOptionViewModel<UpcomingIncomeWindow> option)
        {
            SelectedWindow = option.Value;
            return;
        }

        if (parameter is UpcomingIncomeWindow window)
        {
            SelectedWindow = window;
        }
    }

    private void UpdateWindowSelection()
    {
        foreach (var option in WindowOptions)
        {
            option.IsSelected = option.Value == SelectedWindow;
        }
    }

    // A projection already in the past stays visible in every window: the backend never drops it,
    // and hiding it would silently lose an overdue payment.
    private void ApplyWindowFilter()
    {
        var limit = DateTime.Today.AddDays(SelectedWindowDays);

        Entries.Clear();

        foreach (var entry in _allEntries.Where(entry => entry.ProjectedNextDate.Date <= limit))
        {
            Entries.Add(entry);
        }

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowEntries));
    }

    private void NotifyContentStateChanged()
    {
        OnPropertyChanged(nameof(ShowContent));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowEntries));
    }
}
