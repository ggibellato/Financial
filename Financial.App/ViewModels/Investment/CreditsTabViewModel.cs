using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.Views.Investment;
using OxyPlot;
using System.Windows;

namespace Financial.Presentation.App.ViewModels.Investment;

public class CreditsTabViewModel : ViewModelBase
{
    private readonly ICreditService? _creditService;
    private readonly Func<bool> _hasContext;
    private readonly Func<string> _brokerName;
    private readonly Func<string> _portfolioName;
    private readonly Func<string> _assetName;
    private readonly Action<AssetDetailsDTO> _applyDetails;
    private readonly Action<string, string, MessageBoxImage> _showMessage;

    private readonly RelayCommand _addCreditCommand;
    private readonly RelayCommand _updateCreditCommand;
    private readonly RelayCommand _deleteCreditCommand;
    private readonly RelayCommand _selectCreditsFilterCommand;
    private readonly RelayCommand _selectCreditsTypeModeCommand;
    private readonly RelayCommand _selectCreditsChartTypeCommand;

    private readonly SelectableOptionGroup<PeriodFilter> _creditsFilterGroup;
    private readonly SelectableOptionGroup<CreditsTypeChartMode> _creditsTypeModeGroup;
    private readonly SelectableOptionGroup<CreditsChartType> _creditsChartTypeGroup;
    private PlotModel? _creditsPlotModel;
    private const string DefaultCreditsContextKey = "default";
    private readonly Dictionary<string, CreditsViewState> _creditsViewStateByKey = new(StringComparer.OrdinalIgnoreCase);
    private string _creditsContextKey = DefaultCreditsContextKey;
    private double _creditsPlotWidth;
    private bool _isCreditsAggregateView;
    private CreditDTO? _selectedCredit;
    private IReadOnlyList<CreditsMonthTypeTotals> _creditsChartMonths = Array.Empty<CreditsMonthTypeTotals>();
    private IReadOnlyList<string> _creditsChartTypes = Array.Empty<string>();
    private CreditDialogViewModel? _creditFormViewModel;
    private bool _isCreditFormOpen;

    // Persistent create-form defaults (P38-F10) - read on the next ShowAddCreditFormAsync,
    // written back after every successful add. Not scoped per-asset, matching Web's global
    // sessionStorage-backed persistence.
    private DateTime? _lastUsedCreditDate;
    private string? _lastUsedCreditType;

    public CreditsTabViewModel(
        ICreditService? creditService,
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage)
    {
        _creditService = creditService;
        _hasContext = hasContext ?? throw new ArgumentNullException(nameof(hasContext));
        _brokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
        _portfolioName = portfolioName ?? throw new ArgumentNullException(nameof(portfolioName));
        _assetName = assetName ?? throw new ArgumentNullException(nameof(assetName));
        _applyDetails = applyDetails ?? throw new ArgumentNullException(nameof(applyDetails));
        _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));

        _addCreditCommand = new RelayCommand(AddCredit, CanEditCredits);
        _updateCreditCommand = new RelayCommand(UpdateCredit, CanUpdateCredit);
        _deleteCreditCommand = new RelayCommand(DeleteCredit, CanDeleteCredit);
        _selectCreditsFilterCommand = new RelayCommand(SelectCreditsFilter);
        _selectCreditsTypeModeCommand = new RelayCommand(SelectCreditsTypeMode);
        _selectCreditsChartTypeCommand = new RelayCommand(SelectCreditsChartType);
        _creditsFilterGroup = new SelectableOptionGroup<PeriodFilter>(PeriodFilterHelper.Options, PeriodFilter.Last12Months);
        _creditsTypeModeGroup = new SelectableOptionGroup<CreditsTypeChartMode>(
            [("Stacked", CreditsTypeChartMode.Stacked), ("Grouped", CreditsTypeChartMode.Grouped)],
            CreditsTypeChartMode.Stacked);
        _creditsChartTypeGroup = new SelectableOptionGroup<CreditsChartType>(
            [("Bar", CreditsChartType.Bar), ("Line", CreditsChartType.Line)],
            CreditsChartType.Bar);
    }

    public ObservableCollection<CreditDTO> Credits { get; } = new();
    public ObservableCollection<KeyValuePair<string, decimal>> CreditsByMonthChart { get; } = new();
    public ObservableCollection<SelectableOptionViewModel<PeriodFilter>> CreditsFilters => _creditsFilterGroup.Options;
    public ObservableCollection<SelectableOptionViewModel<CreditsTypeChartMode>> CreditsTypeModes => _creditsTypeModeGroup.Options;
    public ObservableCollection<SelectableOptionViewModel<CreditsChartType>> CreditsChartTypes => _creditsChartTypeGroup.Options;

    public PlotModel? CreditsPlotModel { get => _creditsPlotModel; private set => SetProperty(ref _creditsPlotModel, value); }

    public bool IsCreditsAggregateView
    {
        get => _isCreditsAggregateView;
        private set => SetProperty(ref _isCreditsAggregateView, value);
    }

    public CreditDTO? SelectedCredit
    {
        get => _selectedCredit;
        set { if (SetProperty(ref _selectedCredit, value)) UpdateCommandStates(); }
    }

    public RelayCommand AddCreditCommand => _addCreditCommand;
    public RelayCommand UpdateCreditCommand => _updateCreditCommand;
    public RelayCommand DeleteCreditCommand => _deleteCreditCommand;
    public RelayCommand SelectCreditsFilterCommand => _selectCreditsFilterCommand;
    public RelayCommand SelectCreditsTypeModeCommand => _selectCreditsTypeModeCommand;
    public RelayCommand SelectCreditsChartTypeCommand => _selectCreditsChartTypeCommand;

    public CreditDialogViewModel? CreditFormViewModel
    {
        get => _creditFormViewModel;
        private set => SetProperty(ref _creditFormViewModel, value);
    }

    public bool IsCreditFormOpen
    {
        get => _isCreditFormOpen;
        private set => SetProperty(ref _isCreditFormOpen, value);
    }

    public void Load(string contextKey, IReadOnlyList<CreditDTO> credits)
    {
        IsCreditsAggregateView = false;
        SetCreditsContext(contextKey, rebuild: false);

        Credits.Clear();
        foreach (var credit in credits)
            Credits.Add(credit);
        ApplyCreditsFilter();

        SelectedCredit = null;
    }

    public void LoadAggregate(string contextKey, IReadOnlyList<CreditDTO> credits)
    {
        IsCreditsAggregateView = true;
        SetCreditsContext(contextKey, rebuild: false);

        Credits.Clear();
        foreach (var credit in credits)
            Credits.Add(credit);
        ApplyCreditsFilter();

        SelectedCredit = null;
    }

    public void Clear()
    {
        Credits.Clear();
        CreditsByMonthChart.Clear();
        CreditsPlotModel = null;
        IsCreditsAggregateView = false;
        _creditsContextKey = DefaultCreditsContextKey;
        SelectedCredit = null;
    }

    public void UpdateCommandStates()
    {
        _addCreditCommand.RaiseCanExecuteChanged();
        _updateCreditCommand.RaiseCanExecuteChanged();
        _deleteCreditCommand.RaiseCanExecuteChanged();
    }

    public void UpdatePlotWidth(double plotWidth)
    {
        if (plotWidth <= 0 || CreditsPlotModel == null) return;
        _creditsPlotWidth = plotWidth;
        CreditsChartBuilder.ApplyLabelDensity(CreditsPlotModel, _creditsPlotWidth, _creditsChartMonths, _creditsChartTypes, _creditsTypeModeGroup.SelectedValue, _creditsChartTypeGroup.SelectedValue);
    }

    public async Task Add(Func<Task<CreditDialogData?>> showForm)
    {
        if (!_hasContext())
        {
            ShowInfo("Select an asset before adding a credit.");
            return;
        }

        if (_creditService == null)
        {
            return;
        }

        var dialogData = await showForm();
        if (dialogData == null)
        {
            return;
        }

        if (!CreditTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Credit type must be 'Dividend', 'Rent', or 'JCP'.");
            return;
        }

        var updatedDetails = await _creditService.AddCreditAsync(new CreditCreateDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Date = dialogData.Value.Date,
            Type = normalizedType,
            Value = dialogData.Value.Value
        });

        if (updatedDetails == null)
        {
            ShowWarning("Credit could not be added. Check the values and try again.");
            return;
        }

        _lastUsedCreditDate = dialogData.Value.Date;
        _lastUsedCreditType = normalizedType;

        _applyDetails(updatedDetails);
    }

    public async Task Update(CreditDTO? selectedCredit, Func<Task<CreditDialogData?>> showForm)
    {
        if (_creditService == null || selectedCredit == null)
        {
            return;
        }

        if (selectedCredit.Id == Guid.Empty)
        {
            ShowWarning("Select a saved credit to update.");
            return;
        }

        var dialogData = await showForm();
        if (dialogData == null)
        {
            return;
        }

        if (!CreditTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Credit type must be 'Dividend', 'Rent', or 'JCP'.");
            return;
        }

        var updatedDetails = await _creditService.UpdateCreditAsync(new CreditUpdateDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Id = dialogData.Value.CreditId,
            Date = dialogData.Value.Date,
            Type = normalizedType,
            Value = dialogData.Value.Value
        });

        if (updatedDetails == null)
        {
            ShowWarning("Credit could not be updated. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    public async Task Delete(CreditDTO? selectedCredit, Func<bool> confirmDialog)
    {
        if (selectedCredit == null)
        {
            return;
        }

        if (_creditService == null)
        {
            return;
        }

        if (selectedCredit.Id == Guid.Empty)
        {
            ShowWarning("Select a saved credit to delete.");
            return;
        }

        if (!confirmDialog())
        {
            return;
        }

        var updatedDetails = await _creditService.DeleteCreditAsync(new CreditDeleteDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Id = selectedCredit.Id
        });

        if (updatedDetails == null)
        {
            ShowWarning("Credit could not be deleted. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    private void ShowInfo(string message) => _showMessage(message, "Credit", MessageBoxImage.Information);
    private void ShowWarning(string message) => _showMessage(message, "Credit", MessageBoxImage.Warning);

    private bool CanEditCredits() => _hasContext();
    private bool CanUpdateCredit(object? parameter) => _hasContext() && (parameter is CreditDTO || SelectedCredit != null);
    private bool CanDeleteCredit(object? parameter) => _hasContext() && (parameter is CreditDTO || SelectedCredit != null);

    private async void AddCredit() => await Add(ShowAddCreditFormAsync);

    private async void UpdateCredit(object? parameter)
    {
        if (parameter is CreditDTO credit) SelectedCredit = credit;
        await Update(SelectedCredit, ShowUpdateCreditFormAsync);
    }

    private async void DeleteCredit(object? parameter)
    {
        if (parameter is CreditDTO credit) SelectedCredit = credit;
        await Delete(SelectedCredit, ShowDeleteCreditDialog);
    }

    private Task<CreditDialogData?> ShowCreditFormAsync(CreditDialogViewModel vm)
    {
        var tcs = new TaskCompletionSource<CreditDialogData?>();
        void OnClosed(object? sender, bool? result)
        {
            vm.CloseRequested -= OnClosed;
            IsCreditFormOpen = false;
            CreditFormViewModel = null;
            tcs.SetResult(result == true
                ? new CreditDialogData(vm.CreditId, vm.Date, vm.Type, vm.Value)
                : null);
        }

        vm.CloseRequested += OnClosed;
        CreditFormViewModel = vm;
        IsCreditFormOpen = true;
        return tcs.Task;
    }

    private Task<CreditDialogData?> ShowAddCreditFormAsync() =>
        ShowCreditFormAsync(CreditDialogViewModel.CreateForAdd(
            _brokerName(), _portfolioName(), _assetName(),
            _lastUsedCreditDate ?? DateTime.Today,
            _lastUsedCreditType ?? "Dividend"));

    private Task<CreditDialogData?> ShowUpdateCreditFormAsync()
    {
        if (SelectedCredit == null) return Task.FromResult<CreditDialogData?>(null);
        var vm = CreditDialogViewModel.CreateForUpdate(
            _brokerName(), _portfolioName(), _assetName(),
            SelectedCredit.Id, SelectedCredit.Date, SelectedCredit.Type, SelectedCredit.Value);
        return ShowCreditFormAsync(vm);
    }

    private bool ShowDeleteCreditDialog()
    {
        if (SelectedCredit == null) return false;
        var vm = CreditDialogViewModel.CreateForDelete(
            _brokerName(), _portfolioName(), _assetName(),
            SelectedCredit.Id, SelectedCredit.Date, SelectedCredit.Type, SelectedCredit.Value);
        var dialog = new CreditDialog(vm) { Owner = System.Windows.Application.Current?.MainWindow };
        return dialog.ShowDialog() == true;
    }

    private void SelectCreditsFilter(object? parameter)
    {
        if (SelectableOptionGroup<PeriodFilter>.TryResolve(parameter, out var filter)) SetCreditsFilter(filter);
    }

    private void SelectCreditsTypeMode(object? parameter)
    {
        if (SelectableOptionGroup<CreditsTypeChartMode>.TryResolve(parameter, out var mode)) SetCreditsTypeMode(mode);
    }

    private void SelectCreditsChartType(object? parameter)
    {
        if (SelectableOptionGroup<CreditsChartType>.TryResolve(parameter, out var chartType)) SetCreditsChartType(chartType);
    }

    private void SetCreditsFilter(PeriodFilter filter, bool rebuild = true)
    {
        if (!_creditsFilterGroup.Set(filter)) return;
        UpdateCreditsViewState();
        if (rebuild) ApplyCreditsFilter();
    }

    private void SetCreditsTypeMode(CreditsTypeChartMode mode, bool rebuild = true)
    {
        if (!_creditsTypeModeGroup.Set(mode)) return;
        UpdateCreditsViewState();
        if (rebuild) ApplyCreditsFilter();
    }

    private void SetCreditsChartType(CreditsChartType chartType, bool rebuild = true)
    {
        if (!_creditsChartTypeGroup.Set(chartType)) return;
        UpdateCreditsViewState();
        if (rebuild) ApplyCreditsFilter();
    }

    private void ApplyCreditsFilter()
    {
        RefreshCreditsByMonthChart(FilterCredits(Credits, _creditsFilterGroup.SelectedValue));
    }

    private static IEnumerable<CreditDTO> FilterCredits(IEnumerable<CreditDTO> credits, PeriodFilter filter)
    {
        var (start, endExclusive) = PeriodFilterHelper.GetDateRange(filter, DateTime.Today);
        if (start is null) return credits;
        return credits.Where(credit => credit.Date >= start && credit.Date < endExclusive);
    }

    private void RefreshCreditsByMonthChart(IEnumerable<CreditDTO> credits)
    {
        CreditsByMonthChart.Clear();
        var grouped = credits
            .GroupBy(credit => new DateTime(credit.Date.Year, credit.Date.Month, 1))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var totals = group
                    .GroupBy(credit => credit.Type, StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(typeGroup => typeGroup.Key, StringComparer.CurrentCultureIgnoreCase)
                    .ToDictionary(
                        typeGroup => typeGroup.Key,
                        typeGroup => typeGroup.Sum(credit => credit.Value),
                        StringComparer.CurrentCultureIgnoreCase);
                return new CreditsMonthTypeTotals(group.Key, totals);
            })
            .ToList();

        _creditsChartMonths = grouped;
        _creditsChartTypes = grouped
            .SelectMany(month => month.TotalsByType.Keys)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(type => type, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        foreach (var group in grouped)
            CreditsByMonthChart.Add(new KeyValuePair<string, decimal>(group.Month.ToString("MM/yyyy"), group.Total));

        CreditsPlotModel = CreditsChartBuilder.Build(grouped, _creditsChartTypes, _creditsTypeModeGroup.SelectedValue, _creditsChartTypeGroup.SelectedValue);
        if (CreditsPlotModel != null)
            CreditsChartBuilder.ApplyLabelDensity(CreditsPlotModel, _creditsPlotWidth, _creditsChartMonths, _creditsChartTypes, _creditsTypeModeGroup.SelectedValue, _creditsChartTypeGroup.SelectedValue);
    }

    private void SetCreditsContext(string contextKey, bool rebuild = true)
    {
        _creditsContextKey = string.IsNullOrWhiteSpace(contextKey) ? DefaultCreditsContextKey : contextKey;
        var state = GetCreditsViewState(_creditsContextKey);
        ApplyCreditsViewState(state, rebuild);
    }

    private CreditsViewState GetCreditsViewState(string contextKey)
    {
        if (_creditsViewStateByKey.TryGetValue(contextKey, out var state))
            return state;
        state = new CreditsViewState(PeriodFilter.Last12Months, CreditsTypeChartMode.Stacked, CreditsChartType.Bar);
        _creditsViewStateByKey[contextKey] = state;
        return state;
    }

    private void ApplyCreditsViewState(CreditsViewState state, bool rebuild)
    {
        SetCreditsFilter(state.Filter, rebuild: false);
        SetCreditsTypeMode(state.Mode, rebuild: false);
        SetCreditsChartType(state.ChartType, rebuild: false);
        if (rebuild) ApplyCreditsFilter();
    }

    private void UpdateCreditsViewState()
    {
        if (!string.IsNullOrWhiteSpace(_creditsContextKey))
            _creditsViewStateByKey[_creditsContextKey] = new CreditsViewState(_creditsFilterGroup.SelectedValue, _creditsTypeModeGroup.SelectedValue, _creditsChartTypeGroup.SelectedValue);
    }
}

public readonly record struct CreditDialogData(
    Guid CreditId,
    DateTime Date,
    string Type,
    decimal Value);
