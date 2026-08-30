using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.Views.Investment;
using OxyPlot;
using System.Windows;

namespace Financial.Presentation.App.ViewModels.Investment;

public class PriceHistoryTabViewModel : ViewModelBase
{
    private readonly IAssetPriceHistoryService? _priceService;
    private readonly Func<bool> _hasContext;
    private readonly Func<string> _brokerName;
    private readonly Func<string> _portfolioName;
    private readonly Func<string> _assetName;
    private readonly Action<AssetDetailsDTO> _applyDetails;
    private readonly Action<string, string, MessageBoxImage> _showMessage;

    private readonly RelayCommand _selectPriceHistoryFilterCommand;
    private readonly RelayCommand _addPriceCommand;
    private readonly RelayCommand _updatePriceCommand;
    private readonly RelayCommand _deletePriceCommand;

    private PlotModel? _priceHistoryPlotModel;
    private PeriodFilter _selectedPriceHistoryFilter = PeriodFilter.Last12Months;
    private const string DefaultPriceHistoryContextKey = "default";
    private readonly Dictionary<string, PriceHistoryViewState> _priceHistoryViewStateByKey = new(StringComparer.OrdinalIgnoreCase);
    private string _priceHistoryContextKey = DefaultPriceHistoryContextKey;
    private AssetPriceSnapshotDTO? _selectedPriceEntry;
    private PriceDialogViewModel? _priceFormViewModel;
    private bool _isPriceFormOpen;

    // Persistent create-form default (P38-F10) - read on the next ShowAddPriceFormAsync, written
    // back after every successful set. Not scoped per-asset, matching Web's global
    // sessionStorage-backed persistence.
    private DateTime? _lastUsedPriceDate;

    public PriceHistoryTabViewModel(
        IAssetPriceHistoryService? priceService,
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage)
    {
        _priceService = priceService;
        _hasContext = hasContext ?? throw new ArgumentNullException(nameof(hasContext));
        _brokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
        _portfolioName = portfolioName ?? throw new ArgumentNullException(nameof(portfolioName));
        _assetName = assetName ?? throw new ArgumentNullException(nameof(assetName));
        _applyDetails = applyDetails ?? throw new ArgumentNullException(nameof(applyDetails));
        _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));

        _selectPriceHistoryFilterCommand = new RelayCommand(SelectPriceHistoryFilter);
        _addPriceCommand = new RelayCommand(AddPrice, CanAddPrice);
        _updatePriceCommand = new RelayCommand(UpdatePrice, CanUpdatePrice);
        _deletePriceCommand = new RelayCommand(DeletePrice, CanDeletePrice);
        InitializePriceHistoryFilters();
    }

    public ObservableCollection<AssetPriceSnapshotDTO> PriceHistory { get; } = new();
    public ObservableCollection<SelectableOptionViewModel<PeriodFilter>> PriceHistoryFilters { get; } = new();

    public PlotModel? PriceHistoryPlotModel { get => _priceHistoryPlotModel; private set => SetProperty(ref _priceHistoryPlotModel, value); }

    public AssetPriceSnapshotDTO? SelectedPriceEntry
    {
        get => _selectedPriceEntry;
        set { if (SetProperty(ref _selectedPriceEntry, value)) UpdateCommandStates(); }
    }

    public RelayCommand SelectPriceHistoryFilterCommand => _selectPriceHistoryFilterCommand;
    public RelayCommand AddPriceCommand => _addPriceCommand;
    public RelayCommand UpdatePriceCommand => _updatePriceCommand;
    public RelayCommand DeletePriceCommand => _deletePriceCommand;

    public PriceDialogViewModel? PriceFormViewModel
    {
        get => _priceFormViewModel;
        private set => SetProperty(ref _priceFormViewModel, value);
    }

    public bool IsPriceFormOpen
    {
        get => _isPriceFormOpen;
        private set => SetProperty(ref _isPriceFormOpen, value);
    }

    public void Load(string contextKey, IReadOnlyList<AssetPriceSnapshotDTO> priceHistory)
    {
        SetPriceHistoryContext(contextKey, rebuild: false);
        PriceHistory.Clear();
        foreach (var entry in priceHistory)
            PriceHistory.Add(entry);
        ApplyPriceHistoryFilter();
        SelectedPriceEntry = null;
    }

    public void Clear()
    {
        PriceHistory.Clear();
        PriceHistoryPlotModel = null;
        SelectedPriceEntry = null;
    }

    public void UpdateCommandStates()
    {
        _addPriceCommand.RaiseCanExecuteChanged();
        _updatePriceCommand.RaiseCanExecuteChanged();
        _deletePriceCommand.RaiseCanExecuteChanged();
    }

    public async Task Set(Func<Task<PriceDialogData?>> showForm)
    {
        if (!_hasContext())
        {
            ShowInfo("Select an asset before setting a price.");
            return;
        }

        if (_priceService == null)
        {
            return;
        }

        var dialogData = await showForm();
        if (dialogData == null)
        {
            return;
        }

        var updatedDetails = await _priceService.SetPriceAsync(new SetAssetPriceDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Date = dialogData.Value.Date,
            Price = dialogData.Value.Price
        });

        if (updatedDetails == null)
        {
            ShowWarning("Price could not be saved. Check the values and try again.");
            return;
        }

        _lastUsedPriceDate = dialogData.Value.Date.ToDateTime(TimeOnly.MinValue);

        _applyDetails(updatedDetails);
    }

    public async Task Delete(AssetPriceSnapshotDTO? selectedEntry, Func<bool> confirmDialog)
    {
        if (selectedEntry == null)
        {
            return;
        }

        if (_priceService == null)
        {
            return;
        }

        if (!selectedEntry.IsManual)
        {
            ShowWarning("Only manually-entered prices can be deleted.");
            return;
        }

        if (!confirmDialog())
        {
            return;
        }

        var updatedDetails = await _priceService.DeletePriceAsync(new DeleteAssetPriceDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Date = selectedEntry.Date
        });

        if (updatedDetails == null)
        {
            ShowWarning("Price could not be deleted. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    private void ShowInfo(string message) => _showMessage(message, "Price", MessageBoxImage.Information);
    private void ShowWarning(string message) => _showMessage(message, "Price", MessageBoxImage.Warning);

    private void InitializePriceHistoryFilters()
    {
        PriceHistoryFilters.Clear();
        foreach (var (label, filter) in PeriodFilterHelper.Options)
            PriceHistoryFilters.Add(new SelectableOptionViewModel<PeriodFilter>(label, filter));
        SetPriceHistoryFilter(PeriodFilter.Last12Months, rebuild: false);
    }

    private void SelectPriceHistoryFilter(object? parameter)
    {
        if (parameter is SelectableOptionViewModel<PeriodFilter> option) { SetPriceHistoryFilter(option.Value); return; }
        if (parameter is PeriodFilter filter) SetPriceHistoryFilter(filter);
    }

    private void SetPriceHistoryFilter(PeriodFilter filter, bool rebuild = true)
    {
        if (_selectedPriceHistoryFilter == filter && PriceHistoryFilters.Count > 0)
        {
            UpdatePriceHistoryFilterSelection();
            return;
        }
        _selectedPriceHistoryFilter = filter;
        UpdatePriceHistoryFilterSelection();
        UpdatePriceHistoryViewState();
        if (rebuild) ApplyPriceHistoryFilter();
    }

    private void UpdatePriceHistoryFilterSelection()
    {
        foreach (var option in PriceHistoryFilters)
            option.IsSelected = option.Value == _selectedPriceHistoryFilter;
    }

    private void ApplyPriceHistoryFilter()
    {
        RefreshPriceHistoryChart(FilterPriceHistory(PriceHistory, _selectedPriceHistoryFilter));
    }

    private static IEnumerable<AssetPriceSnapshotDTO> FilterPriceHistory(IEnumerable<AssetPriceSnapshotDTO> entries, PeriodFilter filter)
    {
        var (start, endExclusive) = PeriodFilterHelper.GetDateRange(filter, DateTime.Today);
        if (start is null) return entries;
        return entries.Where(entry =>
        {
            var date = entry.Date.ToDateTime(TimeOnly.MinValue);
            return date >= start && date < endExclusive;
        });
    }

    private void RefreshPriceHistoryChart(IEnumerable<AssetPriceSnapshotDTO> entries)
    {
        PriceHistoryPlotModel = PriceHistoryChartBuilder.Build(entries.ToList());
    }

    private void SetPriceHistoryContext(string contextKey, bool rebuild = true)
    {
        _priceHistoryContextKey = string.IsNullOrWhiteSpace(contextKey) ? DefaultPriceHistoryContextKey : contextKey;
        var state = GetPriceHistoryViewState(_priceHistoryContextKey);
        ApplyPriceHistoryViewState(state, rebuild);
    }

    private PriceHistoryViewState GetPriceHistoryViewState(string contextKey)
    {
        if (_priceHistoryViewStateByKey.TryGetValue(contextKey, out var state))
            return state;
        state = new PriceHistoryViewState(PeriodFilter.Last12Months);
        _priceHistoryViewStateByKey[contextKey] = state;
        return state;
    }

    private void ApplyPriceHistoryViewState(PriceHistoryViewState state, bool rebuild)
    {
        SetPriceHistoryFilter(state.Filter, rebuild: false);
        if (rebuild) ApplyPriceHistoryFilter();
    }

    private void UpdatePriceHistoryViewState()
    {
        if (!string.IsNullOrWhiteSpace(_priceHistoryContextKey))
            _priceHistoryViewStateByKey[_priceHistoryContextKey] = new PriceHistoryViewState(_selectedPriceHistoryFilter);
    }

    private bool CanAddPrice() => _hasContext();
    private bool CanUpdatePrice(object? parameter) => _hasContext() && ResolvePriceEntry(parameter)?.IsManual == true;
    private bool CanDeletePrice(object? parameter) => _hasContext() && ResolvePriceEntry(parameter)?.IsManual == true;

    private AssetPriceSnapshotDTO? ResolvePriceEntry(object? parameter) =>
        parameter as AssetPriceSnapshotDTO ?? SelectedPriceEntry;

    private async void AddPrice() => await Set(ShowAddPriceFormAsync);

    private async void UpdatePrice(object? parameter)
    {
        if (parameter is AssetPriceSnapshotDTO entry) SelectedPriceEntry = entry;
        await Set(ShowUpdatePriceFormAsync);
    }

    private async void DeletePrice(object? parameter)
    {
        if (parameter is AssetPriceSnapshotDTO entry) SelectedPriceEntry = entry;
        await Delete(SelectedPriceEntry, ShowDeletePriceDialog);
    }

    private Task<PriceDialogData?> ShowPriceFormAsync(PriceDialogViewModel vm)
    {
        var tcs = new TaskCompletionSource<PriceDialogData?>();
        void OnClosed(object? sender, bool? result)
        {
            vm.CloseRequested -= OnClosed;
            IsPriceFormOpen = false;
            PriceFormViewModel = null;
            tcs.SetResult(result == true
                ? new PriceDialogData(DateOnly.FromDateTime(vm.Date), vm.Price)
                : null);
        }

        vm.CloseRequested += OnClosed;
        PriceFormViewModel = vm;
        IsPriceFormOpen = true;
        return tcs.Task;
    }

    private Task<PriceDialogData?> ShowAddPriceFormAsync() =>
        ShowPriceFormAsync(PriceDialogViewModel.CreateForAdd(
            _brokerName(), _portfolioName(), _assetName(), _lastUsedPriceDate ?? DateTime.Today));

    private Task<PriceDialogData?> ShowUpdatePriceFormAsync()
    {
        if (SelectedPriceEntry == null) return Task.FromResult<PriceDialogData?>(null);
        var vm = PriceDialogViewModel.CreateForUpdate(
            _brokerName(), _portfolioName(), _assetName(),
            SelectedPriceEntry.Date.ToDateTime(TimeOnly.MinValue), SelectedPriceEntry.Price);
        return ShowPriceFormAsync(vm);
    }

    private bool ShowDeletePriceDialog()
    {
        if (SelectedPriceEntry == null) return false;
        var vm = PriceDialogViewModel.CreateForDelete(
            _brokerName(), _portfolioName(), _assetName(),
            SelectedPriceEntry.Date.ToDateTime(TimeOnly.MinValue), SelectedPriceEntry.Price);
        var dialog = new PriceDialog(vm) { Owner = System.Windows.Application.Current?.MainWindow };
        return dialog.ShowDialog() == true;
    }
}

public readonly record struct PriceDialogData(DateOnly Date, decimal Price);
