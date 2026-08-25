using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.Views.Investment;
using OxyPlot;
using System.Windows;

namespace Financial.Presentation.App.ViewModels.Investment;

public class TransactionsTabViewModel : ViewModelBase
{
    private readonly ITransactionService? _transactionService;
    private readonly ITransactionQueryService _transactionQueryService;
    private readonly InvestmentScope _scope;
    private readonly Func<bool> _hasContext;
    private readonly Func<string> _brokerName;
    private readonly Func<string> _portfolioName;
    private readonly Func<string> _assetName;
    private readonly Action<AssetDetailsDTO> _applyDetails;
    private readonly Action<string, string, MessageBoxImage> _showMessage;

    private readonly RelayCommand _addTransactionCommand;
    private readonly RelayCommand _updateTransactionCommand;
    private readonly RelayCommand _deleteTransactionCommand;
    private readonly RelayCommand _selectTransactionsFilterCommand;
    private readonly RelayCommand _selectTransactionsChartModeCommand;

    private TransactionDialogViewModel? _transactionFormViewModel;
    private bool _isTransactionFormOpen;
    private TransactionDTO? _selectedTransaction;
    private PlotModel? _transactionsPlotModel;
    private PeriodFilter _selectedTransactionsFilter = PeriodFilter.Last12Months;
    private ChartTypeMode _selectedTransactionsChartMode = ChartTypeMode.Bar;
    private const string DefaultTransactionsContextKey = "default";
    private readonly Dictionary<string, TransactionsViewState> _transactionsViewStateByKey = new(StringComparer.OrdinalIgnoreCase);
    private string _transactionsContextKey = DefaultTransactionsContextKey;
    private double _transactionsPlotWidth;
    private bool _isTransactionsAggregateView;
    private bool _isTransactionsLoading;
    private string? _transactionsError;
    private CancellationTokenSource? _transactionsCts;
    private IReadOnlyList<TransactionSummaryItemDTO> _brokerPortfolioTransactions = Array.Empty<TransactionSummaryItemDTO>();
    private IReadOnlyList<TransactionMonthNet> _transactionsChartMonths = Array.Empty<TransactionMonthNet>();

    public TransactionsTabViewModel(
        ITransactionService? transactionService,
        ITransactionQueryService transactionQueryService,
        InvestmentScope scope,
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage)
    {
        _transactionService = transactionService;
        _transactionQueryService = transactionQueryService ?? throw new ArgumentNullException(nameof(transactionQueryService));
        _scope = scope;
        _hasContext = hasContext ?? throw new ArgumentNullException(nameof(hasContext));
        _brokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
        _portfolioName = portfolioName ?? throw new ArgumentNullException(nameof(portfolioName));
        _assetName = assetName ?? throw new ArgumentNullException(nameof(assetName));
        _applyDetails = applyDetails ?? throw new ArgumentNullException(nameof(applyDetails));
        _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));

        _addTransactionCommand = new RelayCommand(AddTransaction, CanEditTransactions);
        _updateTransactionCommand = new RelayCommand(UpdateTransaction, CanUpdateTransaction);
        _deleteTransactionCommand = new RelayCommand(DeleteTransaction, CanDeleteTransaction);
        _selectTransactionsFilterCommand = new RelayCommand(SelectTransactionsFilter);
        _selectTransactionsChartModeCommand = new RelayCommand(SelectTransactionsChartMode);
        InitializeTransactionsFilters();
        InitializeChartTypeModes();
    }

    public ObservableCollection<TransactionDTO> Transactions { get; } = new();

    public PlotModel? TransactionsPlotModel { get => _transactionsPlotModel; private set => SetProperty(ref _transactionsPlotModel, value); }

    public bool IsTransactionsAggregateView
    {
        get => _isTransactionsAggregateView;
        private set => SetProperty(ref _isTransactionsAggregateView, value);
    }

    public bool IsTransactionsLoading
    {
        get => _isTransactionsLoading;
        private set => SetProperty(ref _isTransactionsLoading, value);
    }

    public string? TransactionsError
    {
        get => _transactionsError;
        private set { if (SetProperty(ref _transactionsError, value)) OnPropertyChanged(nameof(HasTransactionsError)); }
    }

    public bool HasTransactionsError => TransactionsError != null;

    public ObservableCollection<TransactionsFilterOptionViewModel> TransactionsFilters { get; } = new();
    public ObservableCollection<ChartTypeModeOptionViewModel> ChartTypeModes { get; } = new();

    public TransactionDTO? SelectedTransaction
    {
        get => _selectedTransaction;
        set { if (SetProperty(ref _selectedTransaction, value)) UpdateCommandStates(); }
    }

    public RelayCommand AddTransactionCommand => _addTransactionCommand;
    public RelayCommand UpdateTransactionCommand => _updateTransactionCommand;
    public RelayCommand DeleteTransactionCommand => _deleteTransactionCommand;
    public RelayCommand SelectTransactionsFilterCommand => _selectTransactionsFilterCommand;
    public RelayCommand SelectTransactionsChartModeCommand => _selectTransactionsChartModeCommand;

    public TransactionDialogViewModel? TransactionFormViewModel
    {
        get => _transactionFormViewModel;
        private set => SetProperty(ref _transactionFormViewModel, value);
    }

    public bool IsTransactionFormOpen
    {
        get => _isTransactionFormOpen;
        private set => SetProperty(ref _isTransactionFormOpen, value);
    }

    public void Load(string contextKey, IReadOnlyList<TransactionDTO> transactions)
    {
        IsTransactionsAggregateView = false;
        CancelAndResetTransactionsFetch();

        Transactions.Clear();
        foreach (var tx in transactions)
            Transactions.Add(tx);

        SetTransactionsContext(contextKey, rebuild: false);
        ApplyTransactionsFilter();

        SelectedTransaction = null;
    }

    public void LoadAggregate(string contextKey)
    {
        Transactions.Clear();
        IsTransactionsAggregateView = true;
        CancelAndResetTransactionsFetch();
        SetTransactionsContext(contextKey, rebuild: false);
        SelectedTransaction = null;
    }

    public void Clear()
    {
        IsTransactionsAggregateView = false;
        CancelAndResetTransactionsFetch();
        _transactionsContextKey = DefaultTransactionsContextKey;
        Transactions.Clear();
        SelectedTransaction = null;
    }

    public void UpdateCommandStates()
    {
        _addTransactionCommand.RaiseCanExecuteChanged();
        _updateTransactionCommand.RaiseCanExecuteChanged();
        _deleteTransactionCommand.RaiseCanExecuteChanged();
    }

    public void UpdatePlotWidth(double plotWidth)
    {
        if (plotWidth <= 0 || TransactionsPlotModel == null) return;
        _transactionsPlotWidth = plotWidth;
        TransactionsChartBuilder.ApplyLabelDensity(TransactionsPlotModel, _transactionsPlotWidth, _transactionsChartMonths);
    }

    public virtual Task LoadBroker(string brokerName)
    {
        CancelAndResetTransactionsFetch();
        IsTransactionsLoading = true;

        _transactionsCts = new CancellationTokenSource();
        var token = _transactionsCts.Token;
        return Task.Run(() =>
        {
            try
            {
                var transactions = _transactionQueryService.GetTransactionsByBroker(brokerName, _scope);
                if (token.IsCancellationRequested) return;
                ApplyFetchedTransactions(transactions);
            }
            catch
            {
                if (token.IsCancellationRequested) return;
                TransactionsError = "Unable to load transactions";
                IsTransactionsLoading = false;
            }
        }, token);
    }

    public virtual Task LoadPortfolio(string brokerName, string portfolioName)
    {
        CancelAndResetTransactionsFetch();
        IsTransactionsLoading = true;

        _transactionsCts = new CancellationTokenSource();
        var token = _transactionsCts.Token;
        return Task.Run(() =>
        {
            try
            {
                var transactions = _transactionQueryService.GetTransactionsByPortfolio(brokerName, portfolioName, _scope);
                if (token.IsCancellationRequested) return;
                ApplyFetchedTransactions(transactions);
            }
            catch
            {
                if (token.IsCancellationRequested) return;
                TransactionsError = "Unable to load transactions";
                IsTransactionsLoading = false;
            }
        }, token);
    }

    public async Task Add(Func<Task<TransactionDialogData?>> showForm)
    {
        if (!_hasContext())
        {
            ShowInfo("Select an asset before adding a transaction.");
            return;
        }

        if (_transactionService == null)
        {
            return;
        }

        var dialogData = await showForm();
        if (dialogData == null)
        {
            return;
        }

        if (!TransactionTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Transaction type must be 'Buy' or 'Sell'.");
            return;
        }

        var updatedDetails = await _transactionService.AddTransactionAsync(new TransactionCreateDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Date = dialogData.Value.Date,
            Type = normalizedType,
            Quantity = dialogData.Value.Quantity,
            UnitPrice = dialogData.Value.UnitPrice,
            Fees = dialogData.Value.Fees
        });

        if (updatedDetails == null)
        {
            ShowWarning("Transaction could not be added. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    public async Task Update(TransactionDTO? selectedTransaction, Func<Task<TransactionDialogData?>> showForm)
    {
        if (_transactionService == null || selectedTransaction == null)
        {
            return;
        }

        if (selectedTransaction.Id == Guid.Empty)
        {
            ShowWarning("Select a saved transaction to update.");
            return;
        }

        var dialogData = await showForm();
        if (dialogData == null)
        {
            return;
        }

        if (!TransactionTypeParser.TryNormalize(dialogData.Value.Type, out var normalizedType))
        {
            ShowWarning("Transaction type must be 'Buy' or 'Sell'.");
            return;
        }

        var updatedDetails = await _transactionService.UpdateTransactionAsync(new TransactionUpdateDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Id = dialogData.Value.TransactionId,
            Date = dialogData.Value.Date,
            Type = normalizedType,
            Quantity = dialogData.Value.Quantity,
            UnitPrice = dialogData.Value.UnitPrice,
            Fees = dialogData.Value.Fees
        });

        if (updatedDetails == null)
        {
            ShowWarning("Transaction could not be updated. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    public async Task Delete(TransactionDTO? selectedTransaction, Func<bool> confirmDialog)
    {
        if (selectedTransaction == null)
        {
            return;
        }

        if (_transactionService == null)
        {
            return;
        }

        if (selectedTransaction.Id == Guid.Empty)
        {
            ShowWarning("Select a saved transaction to delete.");
            return;
        }

        if (!confirmDialog())
        {
            return;
        }

        var updatedDetails = await _transactionService.DeleteTransactionAsync(new TransactionDeleteDTO
        {
            BrokerName = _brokerName(),
            PortfolioName = _portfolioName(),
            AssetName = _assetName(),
            Id = selectedTransaction.Id
        });

        if (updatedDetails == null)
        {
            ShowWarning("Transaction could not be deleted. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    private void ShowInfo(string message) => _showMessage(message, "Transaction", MessageBoxImage.Information);
    private void ShowWarning(string message) => _showMessage(message, "Transaction", MessageBoxImage.Warning);

    private bool CanEditTransactions() => _hasContext();
    private bool CanUpdateTransaction(object? parameter) => _hasContext() && (parameter is TransactionDTO || SelectedTransaction != null);
    private bool CanDeleteTransaction(object? parameter) => _hasContext() && (parameter is TransactionDTO || SelectedTransaction != null);

    private async void AddTransaction() => await Add(ShowAddTransactionFormAsync);

    private async void UpdateTransaction(object? parameter)
    {
        if (parameter is TransactionDTO tx) SelectedTransaction = tx;
        await Update(SelectedTransaction, ShowUpdateTransactionFormAsync);
    }

    private async void DeleteTransaction(object? parameter)
    {
        if (parameter is TransactionDTO tx) SelectedTransaction = tx;
        await Delete(SelectedTransaction, ShowDeleteTransactionDialog);
    }

    // "New X" / edit actions open an inline form on the same tab instead of a
    // modal dialog (docs/ui/forms-data-and-visualisations.md's "'New X' create
    // actions are inline forms, not popup dialogs" rule) — the form's
    // ConfirmCommand/CancelCommand raise CloseRequested exactly like the old
    // dialog did, so this just awaits that event instead of a blocking
    // ShowDialog() call. Delete stays a real (confirmation) dialog below.
    private Task<TransactionDialogData?> ShowTransactionFormAsync(TransactionDialogViewModel vm)
    {
        var tcs = new TaskCompletionSource<TransactionDialogData?>();
        void OnClosed(object? sender, bool? result)
        {
            vm.CloseRequested -= OnClosed;
            IsTransactionFormOpen = false;
            TransactionFormViewModel = null;
            tcs.SetResult(result == true
                ? new TransactionDialogData(vm.TransactionId, vm.Date, vm.Type, vm.Quantity, vm.UnitPrice, vm.Fees)
                : null);
        }

        vm.CloseRequested += OnClosed;
        TransactionFormViewModel = vm;
        IsTransactionFormOpen = true;
        return tcs.Task;
    }

    private Task<TransactionDialogData?> ShowAddTransactionFormAsync() =>
        ShowTransactionFormAsync(TransactionDialogViewModel.CreateForAdd(_brokerName(), _portfolioName(), _assetName()));

    private Task<TransactionDialogData?> ShowUpdateTransactionFormAsync()
    {
        if (SelectedTransaction == null) return Task.FromResult<TransactionDialogData?>(null);
        var vm = TransactionDialogViewModel.CreateForUpdate(
            _brokerName(), _portfolioName(), _assetName(),
            SelectedTransaction.Id, SelectedTransaction.Date, SelectedTransaction.Type,
            SelectedTransaction.Quantity, SelectedTransaction.UnitPrice, SelectedTransaction.Fees);
        return ShowTransactionFormAsync(vm);
    }

    private bool ShowDeleteTransactionDialog()
    {
        if (SelectedTransaction == null) return false;
        var vm = TransactionDialogViewModel.CreateForDelete(
            _brokerName(), _portfolioName(), _assetName(),
            SelectedTransaction.Id, SelectedTransaction.Date, SelectedTransaction.Type,
            SelectedTransaction.Quantity, SelectedTransaction.UnitPrice, SelectedTransaction.Fees);
        var dialog = new TransactionDialog(vm) { Owner = System.Windows.Application.Current?.MainWindow };
        return dialog.ShowDialog() == true;
    }

    private void ApplyFetchedTransactions(IReadOnlyList<TransactionSummaryItemDTO> transactions)
    {
        _brokerPortfolioTransactions = transactions;
        ApplyTransactionsFilter();
        IsTransactionsLoading = false;
    }

    private void CancelAndResetTransactionsFetch()
    {
        _transactionsCts?.Cancel();
        _transactionsCts?.Dispose();
        _transactionsCts = null;
        IsTransactionsLoading = false;
        TransactionsError = null;
        TransactionsPlotModel = null;
        _brokerPortfolioTransactions = Array.Empty<TransactionSummaryItemDTO>();
        _transactionsChartMonths = Array.Empty<TransactionMonthNet>();
    }

    private void InitializeTransactionsFilters()
    {
        TransactionsFilters.Clear();
        foreach (var (label, filter) in PeriodFilterHelper.Options)
            TransactionsFilters.Add(new TransactionsFilterOptionViewModel(label, filter));
        SetTransactionsFilter(PeriodFilter.Last12Months, rebuild: false);
    }

    private void InitializeChartTypeModes()
    {
        ChartTypeModes.Clear();
        ChartTypeModes.Add(new ChartTypeModeOptionViewModel("Bar", ChartTypeMode.Bar));
        ChartTypeModes.Add(new ChartTypeModeOptionViewModel("Line", ChartTypeMode.Line));
        SetTransactionsChartMode(ChartTypeMode.Bar, rebuild: false);
    }

    private void SelectTransactionsFilter(object? parameter)
    {
        if (parameter is TransactionsFilterOptionViewModel option) { SetTransactionsFilter(option.Filter); return; }
        if (parameter is PeriodFilter filter) SetTransactionsFilter(filter);
    }

    private void SelectTransactionsChartMode(object? parameter)
    {
        if (parameter is ChartTypeModeOptionViewModel option) { SetTransactionsChartMode(option.Mode); return; }
        if (parameter is ChartTypeMode mode) SetTransactionsChartMode(mode);
    }

    private void SetTransactionsFilter(PeriodFilter filter, bool rebuild = true)
    {
        if (_selectedTransactionsFilter == filter && TransactionsFilters.Count > 0)
        {
            UpdateTransactionsFilterSelection();
            return;
        }
        _selectedTransactionsFilter = filter;
        UpdateTransactionsFilterSelection();
        UpdateTransactionsViewState();
        if (rebuild) ApplyTransactionsFilter();
    }

    private void SetTransactionsChartMode(ChartTypeMode mode, bool rebuild = true)
    {
        if (_selectedTransactionsChartMode == mode && ChartTypeModes.Count > 0)
        {
            UpdateTransactionsChartModeSelection();
            return;
        }
        _selectedTransactionsChartMode = mode;
        UpdateTransactionsChartModeSelection();
        UpdateTransactionsViewState();
        if (rebuild) ApplyTransactionsFilter();
    }

    private void UpdateTransactionsFilterSelection()
    {
        foreach (var option in TransactionsFilters)
            option.IsSelected = option.Filter == _selectedTransactionsFilter;
    }

    private void UpdateTransactionsChartModeSelection()
    {
        foreach (var option in ChartTypeModes)
            option.IsSelected = option.Mode == _selectedTransactionsChartMode;
    }

    private void ApplyTransactionsFilter()
    {
        IEnumerable<(DateTime Date, string Type, decimal TotalPrice)> source = IsTransactionsAggregateView
            ? _brokerPortfolioTransactions.Select(t => (t.Date, t.Type, t.TotalPrice))
            : Transactions.Select(t => (t.Date, t.Type, t.TotalPrice));

        var months = TransactionsMonthlyAggregator.BuildMonthlyNetInvested(source, _selectedTransactionsFilter, DateTime.Today);
        _transactionsChartMonths = months;
        TransactionsPlotModel = TransactionsChartBuilder.Build(months, _selectedTransactionsChartMode);
        if (TransactionsPlotModel != null)
            TransactionsChartBuilder.ApplyLabelDensity(TransactionsPlotModel, _transactionsPlotWidth, months);
    }

    private void SetTransactionsContext(string contextKey, bool rebuild = true)
    {
        _transactionsContextKey = string.IsNullOrWhiteSpace(contextKey) ? DefaultTransactionsContextKey : contextKey;
        var state = GetTransactionsViewState(_transactionsContextKey);
        ApplyTransactionsViewState(state, rebuild);
    }

    private TransactionsViewState GetTransactionsViewState(string contextKey)
    {
        if (_transactionsViewStateByKey.TryGetValue(contextKey, out var state))
            return state;
        state = new TransactionsViewState(PeriodFilter.Last12Months, ChartTypeMode.Bar);
        _transactionsViewStateByKey[contextKey] = state;
        return state;
    }

    private void ApplyTransactionsViewState(TransactionsViewState state, bool rebuild)
    {
        SetTransactionsFilter(state.Filter, rebuild: false);
        SetTransactionsChartMode(state.Mode, rebuild: false);
        if (rebuild) ApplyTransactionsFilter();
    }

    private void UpdateTransactionsViewState()
    {
        if (!string.IsNullOrWhiteSpace(_transactionsContextKey))
            _transactionsViewStateByKey[_transactionsContextKey] = new TransactionsViewState(_selectedTransactionsFilter, _selectedTransactionsChartMode);
    }
}

public readonly record struct TransactionDialogData(
    Guid TransactionId,
    DateTime Date,
    string Type,
    decimal Quantity,
    decimal UnitPrice,
    decimal Fees);
