using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.ViewModels.Investment;
using Financial.Shared.Abstractions.Currencies;
using Microsoft.Extensions.Logging;
using OxyPlot;

namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class AllocationBreakdownViewModel : ViewModelBase
{
    private readonly IAllocationBreakdownService _allocationService;
    private readonly ILogger<AllocationBreakdownViewModel> _logger;

    private AllocationBreakdownDTO? _breakdown;
    private AllocationDimension _selectedDimension = AllocationDimension.Class;
    private PlotModel _plotModel = new();
    private bool _isLoading;
    private string? _errorMessage;
    private int _requestId;
    private Currency? _brokerCurrencyFilter;

    public AllocationBreakdownViewModel(
        IAllocationBreakdownService allocationService,
        ILogger<AllocationBreakdownViewModel> logger)
    {
        _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RefreshCommand = new RelayCommand(async () => await LoadAsync());
    }

    public RelayCommand RefreshCommand { get; }

    public ObservableCollection<AllocationEntryRowViewModel> Entries { get; } = [];

    public AllocationDimension SelectedDimension
    {
        get => _selectedDimension;
        set
        {
            if (SetProperty(ref _selectedDimension, value))
            {
                OnPropertyChanged(nameof(ChartTitle));
                RebuildSelectedDimension();
            }
        }
    }

    public PlotModel PlotModel
    {
        get => _plotModel;
        private set => SetProperty(ref _plotModel, value);
    }

    public string ChartTitle => SelectedDimension switch
    {
        AllocationDimension.Currency => "Allocation by currency",
        AllocationDimension.Country => "Allocation by country",
        AllocationDimension.Broker => "Allocation by broker",
        _ => "Allocation by asset class",
    };

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(ShowContent));
                OnPropertyChanged(nameof(ShowChart));
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
                OnPropertyChanged(nameof(ShowContent));
                OnPropertyChanged(nameof(ShowChart));
            }
        }
    }

    public bool HasError => ErrorMessage != null;

    public bool ShowContent => !IsLoading && !HasError;

    public bool IsEmpty => Entries.Count == 0;

    public bool ShowChart => ShowContent && !IsEmpty && !IsUnavailable;

    public bool IsUnavailable => _breakdown?.IsUnavailable ?? false;

    public bool ShowDimensionTabs => ShowContent && !IsUnavailable;

    public bool IsPartial => _breakdown?.IsPartial ?? false;

    public string? DisplayCurrencyLabel => _breakdown?.DisplayCurrency;

    public string? EmptyMessage =>
        !IsEmpty ? null :
        _brokerCurrencyFilter is not null ? "No brokers use the selected currency" :
        "No priced holdings to display for this view.";

    public Task LoadAsync(Currency? displayCurrency = null, Currency? brokerCurrencyFilter = null) => ExecuteRefreshAsync(
        () => ++_requestId,
        id => id == _requestId,
        loading => IsLoading = loading,
        error => ErrorMessage = error,
        async isCurrent =>
        {
            var breakdown = await _allocationService.GetAllocationBreakdownAsync(displayCurrency, brokerCurrencyFilter);

            if (isCurrent())
            {
                _breakdown = breakdown;
                _brokerCurrencyFilter = brokerCurrencyFilter;
                RebuildSelectedDimension();
            }
        },
        ex => _logger.LogError("Allocation breakdown refresh failed with {ErrorType}", ex.GetType().Name));

    private void RebuildSelectedDimension()
    {
        Entries.Clear();

        foreach (var entry in SelectedDimensionEntries())
        {
            Entries.Add(entry);
        }

        PlotModel = BrokerBreakdownChartBuilder.Build(
            Entries.Select(entry => (entry.Label, entry.MarketValue)).ToList());

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ShowChart));
        OnPropertyChanged(nameof(ShowDimensionTabs));
        OnPropertyChanged(nameof(IsPartial));
        OnPropertyChanged(nameof(IsUnavailable));
        OnPropertyChanged(nameof(DisplayCurrencyLabel));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private IEnumerable<AllocationEntryRowViewModel> SelectedDimensionEntries()
    {
        if (_breakdown is null)
        {
            return [];
        }

        return SelectedDimension switch
        {
            AllocationDimension.Currency => _breakdown.ByCurrency.Select(entry =>
                new AllocationEntryRowViewModel(entry.Currency, entry.MarketValue, entry.Percentage)),
            AllocationDimension.Country => _breakdown.ByCountry.Select(entry =>
                new AllocationEntryRowViewModel(entry.Country.ToString(), entry.MarketValue, entry.Percentage)),
            AllocationDimension.Broker => _breakdown.ByBroker.Select(entry =>
                new AllocationEntryRowViewModel(entry.BrokerName, entry.MarketValue, entry.Percentage)),
            _ => _breakdown.ByClass.Select(entry =>
                new AllocationEntryRowViewModel(entry.Class.ToString(), entry.MarketValue, entry.Percentage)),
        };
    }
}
