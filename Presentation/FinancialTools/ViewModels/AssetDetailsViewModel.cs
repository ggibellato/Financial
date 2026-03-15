using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.Shared.ViewModels;
using Financial.Presentation.Tools;

namespace Financial.Presentation.Tools.ViewModels;

/// <summary>
/// ViewModel for displaying asset details with operations and credits in tabs
/// </summary>
public class AssetDetailsViewModel : ViewModelBase, IAssetDetailsViewModel
{
    private readonly IOperationService? _operationService;
    private readonly ICreditService? _creditService;
    private readonly IAssetPriceService? _assetPriceService;
    private readonly TodayInfoTracker _todayInfo;
    private readonly OperationActions _operationActions;
    private readonly CreditActions _creditActions;
    private readonly RelayCommand _addOperationCommand;
    private readonly RelayCommand _updateOperationCommand;
    private readonly RelayCommand _deleteOperationCommand;
    private readonly RelayCommand _addCreditCommand;
    private readonly RelayCommand _updateCreditCommand;
    private readonly RelayCommand _deleteCreditCommand;
    private readonly RelayCommand _refreshTodayInfoCommand;
    private readonly RelayCommand _copyAssetNameCommand;
    private string _assetName = string.Empty;
    private string _brokerName = string.Empty;
    private string _portfolioName = string.Empty;
    private string _ticker = string.Empty;
    private string _isin = string.Empty;
    private string _exchange = string.Empty;
    private decimal _quantity;
    private decimal _averagePrice;
    private bool _isActive;
    private decimal _totalBought;
    private decimal _totalSold;
    private decimal _totalCredits;
    private decimal _todayCurrentValue;
    private string _todayCurrentValueAsOf = string.Empty;
    private string _todayInfoMessage = string.Empty;
    private OperationDTO? _selectedOperation;
    private CreditDTO? _selectedCredit;

    public string AssetName
    {
        get => _assetName;
        private set => SetProperty(ref _assetName, value);
    }

    public string BrokerName
    {
        get => _brokerName;
        private set => SetProperty(ref _brokerName, value);
    }

    public string PortfolioName
    {
        get => _portfolioName;
        private set => SetProperty(ref _portfolioName, value);
    }

    public string Ticker
    {
        get => _ticker;
        private set => SetProperty(ref _ticker, value);
    }

    public string ISIN
    {
        get => _isin;
        private set => SetProperty(ref _isin, value);
    }

    public string Exchange
    {
        get => _exchange;
        private set => SetProperty(ref _exchange, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        private set
        {
            if (SetProperty(ref _quantity, value))
            {
                NotifyCurrentValueChanged();
            }
        }
    }

    public decimal AveragePrice
    {
        get => _averagePrice;
        private set
        {
            if (SetProperty(ref _averagePrice, value))
            {
                NotifyCurrentValueChanged();
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public decimal TotalBought
    {
        get => _totalBought;
        private set
        {
            if (SetProperty(ref _totalBought, value))
            {
                OnPropertyChanged(nameof(Balance));
            }
        }
    }

    public decimal TotalSold
    {
        get => _totalSold;
        private set
        {
            if (SetProperty(ref _totalSold, value))
            {
                OnPropertyChanged(nameof(Balance));
            }
        }
    }

    public decimal TotalCredits
    {
        get => _totalCredits;
        private set
        {
            if (SetProperty(ref _totalCredits, value))
            {
                NotifyCurrentValueChanged();
            }
        }
    }

    public decimal Balance => TotalBought - TotalSold;

    public decimal TodayCurrentValue
    {
        get => _todayCurrentValue;
        private set
        {
            if (SetProperty(ref _todayCurrentValue, value))
            {
                NotifyCurrentValueChanged();
            }
        }
    }

    public string TodayCurrentValueAsOf
    {
        get => _todayCurrentValueAsOf;
        private set => SetProperty(ref _todayCurrentValueAsOf, value);
    }

    public string TodayInfoMessage
    {
        get => _todayInfoMessage;
        private set => SetProperty(ref _todayInfoMessage, value);
    }

    public decimal TotalCurrentValue => AssetDetailsCalculations.CalculateTotalCurrentValue(TodayCurrentValue, Quantity);

    public decimal ResultPercent =>
        AssetDetailsCalculations.CalculateResultPercent(AveragePrice, Quantity, TotalCurrentValue);

    public decimal TotalCurrentValueWithCredits => TotalCurrentValue + TotalCredits;

    public decimal ResultPercentWithCredits =>
        AssetDetailsCalculations.CalculateResultPercentWithCredits(AveragePrice, Quantity, TotalCurrentValueWithCredits);

    public bool HasAveragePrice => AssetDetailsCalculations.HasAveragePrice(AveragePrice, Quantity);

    public ObservableCollection<OperationDTO> Operations { get; } = new();
    public ObservableCollection<CreditDTO> Credits { get; } = new();

    public OperationDTO? SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (SetProperty(ref _selectedOperation, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public CreditDTO? SelectedCredit
    {
        get => _selectedCredit;
        set
        {
            if (SetProperty(ref _selectedCredit, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public RelayCommand AddOperationCommand => _addOperationCommand;
    public RelayCommand UpdateOperationCommand => _updateOperationCommand;
    public RelayCommand DeleteOperationCommand => _deleteOperationCommand;
    public RelayCommand AddCreditCommand => _addCreditCommand;
    public RelayCommand UpdateCreditCommand => _updateCreditCommand;
    public RelayCommand DeleteCreditCommand => _deleteCreditCommand;
    public RelayCommand RefreshTodayInfoCommand => _refreshTodayInfoCommand;
    public RelayCommand CopyAssetNameCommand => _copyAssetNameCommand;

    public AssetDetailsViewModel(IOperationService? operationService = null, ICreditService? creditService = null, IAssetPriceService? assetPriceService = null)
    {
        _operationService = operationService;
        _creditService = creditService;
        _assetPriceService = assetPriceService;
        _todayInfo = new TodayInfoTracker(ApplyTodayInfo, ResetTodayInfo, UpdateCommandStates);
        _operationActions = new OperationActions(
            _operationService,
            () => HasOperationContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            LoadAssetDetails,
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        _creditActions = new CreditActions(
            _creditService,
            () => HasCreditContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            LoadAssetDetails,
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        _addOperationCommand = new RelayCommand(AddOperation, CanEditOperations);
        _updateOperationCommand = new RelayCommand(UpdateOperation, CanUpdateOperation);
        _deleteOperationCommand = new RelayCommand(DeleteOperation, CanDeleteOperation);
        _addCreditCommand = new RelayCommand(AddCredit, CanEditCredits);
        _updateCreditCommand = new RelayCommand(UpdateCredit, CanUpdateCredit);
        _deleteCreditCommand = new RelayCommand(DeleteCredit, CanDeleteCredit);
        _refreshTodayInfoCommand = new RelayCommand(RefreshTodayInfo, CanRefreshTodayInfo);
        _copyAssetNameCommand = new RelayCommand(CopyAssetName, CanCopyAssetName);
    }

    /// <summary>
    /// Loads asset details from DTO
    /// </summary>
    public void LoadAssetDetails(AssetDetailsDTO details)
    {
        var assetKey = BuildAssetKey(details.BrokerName, details.PortfolioName, details.Name);
        _todayInfo.UpdateAssetKey(assetKey);

        AssetName = details.Name;
        BrokerName = details.BrokerName;
        PortfolioName = details.PortfolioName;
        Ticker = details.Ticker;
        ISIN = details.ISIN;
        Exchange = details.Exchange;
        Quantity = details.Quantity;
        AveragePrice = details.AveragePrice;
        IsActive = details.IsActive;
        TotalBought = details.TotalBought;
        TotalSold = details.TotalSold;
        TotalCredits = details.TotalCredits;

        Operations.Clear();
        foreach (var op in details.Operations)
        {
            Operations.Add(op);
        }

        Credits.Clear();
        foreach (var credit in details.Credits)
        {
            Credits.Add(credit);
        }

        SelectedOperation = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    /// <summary>
    /// Clears all asset details
    /// </summary>
    public void Clear()
    {
        AssetName = string.Empty;
        BrokerName = string.Empty;
        PortfolioName = string.Empty;
        Ticker = string.Empty;
        ISIN = string.Empty;
        Exchange = string.Empty;
        Quantity = 0;
        AveragePrice = 0;
        IsActive = false;
        TotalBought = 0;
        TotalSold = 0;
        TotalCredits = 0;
        _todayInfo.Clear();
        Operations.Clear();
        Credits.Clear();
        SelectedOperation = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    private bool HasAssetContext =>
        !string.IsNullOrWhiteSpace(BrokerName) &&
        !string.IsNullOrWhiteSpace(PortfolioName) &&
        !string.IsNullOrWhiteSpace(AssetName);

    private bool HasOperationContext => _operationService != null && HasAssetContext;

    private bool HasCreditContext => _creditService != null && HasAssetContext;

    private bool CanEditOperations() => HasOperationContext;

    private bool CanUpdateOperation(object? parameter) =>
        HasOperationContext && (parameter is OperationDTO || SelectedOperation != null);

    private bool CanDeleteOperation(object? parameter) =>
        HasOperationContext && (parameter is OperationDTO || SelectedOperation != null);

    private bool CanEditCredits() => HasCreditContext;

    private bool CanUpdateCredit(object? parameter) =>
        HasCreditContext && (parameter is CreditDTO || SelectedCredit != null);

    private bool CanDeleteCredit(object? parameter) =>
        HasCreditContext && (parameter is CreditDTO || SelectedCredit != null);

    private bool CanRefreshTodayInfo() => _todayInfo.CanRefresh(HasAssetContext);

    private bool CanCopyAssetName() => HasAssetContext;

    private void CopyAssetName()
    {
        if (!HasAssetContext)
        {
            MessageBox.Show("Select an asset before copying.", "Copy Asset", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(AssetName);
    }

    public Task EnsureTodayInfoLoadedAsync()
    {
        return RefreshTodayInfoAsync(forceRefresh: false);
    }

    private async void RefreshTodayInfo()
    {
        await RefreshTodayInfoAsync(forceRefresh: true);
    }

    private Task RefreshTodayInfoAsync(bool forceRefresh)
    {
        return _todayInfo.RefreshAsync(
            forceRefresh,
            HasAssetContext,
            _assetPriceService,
            Exchange,
            Ticker,
            message => TodayInfoMessage = message);
    }

    private void ResetTodayInfo()
    {
        TodayCurrentValue = 0;
        TodayCurrentValueAsOf = string.Empty;
        TodayInfoMessage = string.Empty;
    }

    private void ApplyTodayInfo(TodayInfoSnapshot snapshot)
    {
        TodayCurrentValue = snapshot.Price;
        TodayCurrentValueAsOf = snapshot.AsOf;
        TodayInfoMessage = string.Empty;
    }

    private void NotifyCurrentValueChanged()
    {
        OnPropertyChanged(nameof(TotalCurrentValue));
        OnPropertyChanged(nameof(ResultPercent));
        OnPropertyChanged(nameof(HasAveragePrice));
        OnPropertyChanged(nameof(TotalCurrentValueWithCredits));
        OnPropertyChanged(nameof(ResultPercentWithCredits));
    }

    private static string BuildAssetKey(string brokerName, string portfolioName, string assetName) =>
        $"{brokerName}|{portfolioName}|{assetName}";

    private void AddOperation()
    {
        _operationActions.Add(ShowAddOperationDialog);
    }

    private void AddCredit()
    {
        _creditActions.Add(ShowAddCreditDialog);
    }

    private void UpdateOperation(object? parameter)
    {
        if (parameter is OperationDTO operation)
        {
            SelectedOperation = operation;
        }

        _operationActions.Update(SelectedOperation, ShowUpdateOperationDialog);
    }

    private void UpdateCredit(object? parameter)
    {
        if (parameter is CreditDTO credit)
        {
            SelectedCredit = credit;
        }

        _creditActions.Update(SelectedCredit, ShowUpdateCreditDialog);
    }

    private void DeleteOperation(object? parameter)
    {
        if (parameter is OperationDTO operation)
        {
            SelectedOperation = operation;
        }

        _operationActions.Delete(SelectedOperation, ShowDeleteOperationDialog);
    }

    private void DeleteCredit(object? parameter)
    {
        if (parameter is CreditDTO credit)
        {
            SelectedCredit = credit;
        }

        _creditActions.Delete(SelectedCredit, ShowDeleteCreditDialog);
    }

    private void UpdateCommandStates()
    {
        _addOperationCommand.RaiseCanExecuteChanged();
        _updateOperationCommand.RaiseCanExecuteChanged();
        _deleteOperationCommand.RaiseCanExecuteChanged();
        _addCreditCommand.RaiseCanExecuteChanged();
        _updateCreditCommand.RaiseCanExecuteChanged();
        _deleteCreditCommand.RaiseCanExecuteChanged();
        _refreshTodayInfoCommand.RaiseCanExecuteChanged();
        _copyAssetNameCommand.RaiseCanExecuteChanged();
    }

    private bool ShowOperationDialog(OperationDialogViewModel viewModel)
    {
        var dialog = new OperationDialog(viewModel)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true;
    }

    private OperationDialogData? ShowAddOperationDialog()
    {
        var dialogViewModel = OperationDialogViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        if (!ShowOperationDialog(dialogViewModel))
        {
            return null;
        }

        return new OperationDialogData(
            dialogViewModel.OperationId,
            dialogViewModel.Date,
            dialogViewModel.Type,
            dialogViewModel.Quantity,
            dialogViewModel.UnitPrice,
            dialogViewModel.Fees);
    }

    private OperationDialogData? ShowUpdateOperationDialog()
    {
        if (SelectedOperation == null)
        {
            return null;
        }

        var dialogViewModel = OperationDialogViewModel.CreateForUpdate(
            BrokerName,
            PortfolioName,
            AssetName,
            SelectedOperation.Id,
            SelectedOperation.Date,
            SelectedOperation.Type,
            SelectedOperation.Quantity,
            SelectedOperation.UnitPrice,
            SelectedOperation.Fees);

        if (!ShowOperationDialog(dialogViewModel))
        {
            return null;
        }

        return new OperationDialogData(
            dialogViewModel.OperationId,
            dialogViewModel.Date,
            dialogViewModel.Type,
            dialogViewModel.Quantity,
            dialogViewModel.UnitPrice,
            dialogViewModel.Fees);
    }

    private bool ShowDeleteOperationDialog()
    {
        if (SelectedOperation == null)
        {
            return false;
        }

        var dialogViewModel = OperationDialogViewModel.CreateForDelete(
            BrokerName,
            PortfolioName,
            AssetName,
            SelectedOperation.Id,
            SelectedOperation.Date,
            SelectedOperation.Type,
            SelectedOperation.Quantity,
            SelectedOperation.UnitPrice,
            SelectedOperation.Fees);

        return ShowOperationDialog(dialogViewModel);
    }

    private bool ShowCreditDialog(CreditDialogViewModel viewModel)
    {
        var dialog = new CreditDialog(viewModel)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true;
    }

    private CreditDialogData? ShowAddCreditDialog()
    {
        var dialogViewModel = CreditDialogViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        if (!ShowCreditDialog(dialogViewModel))
        {
            return null;
        }

        return new CreditDialogData(
            dialogViewModel.CreditId,
            dialogViewModel.Date,
            dialogViewModel.Type,
            dialogViewModel.Value);
    }

    private CreditDialogData? ShowUpdateCreditDialog()
    {
        if (SelectedCredit == null)
        {
            return null;
        }

        var dialogViewModel = CreditDialogViewModel.CreateForUpdate(
            BrokerName,
            PortfolioName,
            AssetName,
            SelectedCredit.Id,
            SelectedCredit.Date,
            SelectedCredit.Type,
            SelectedCredit.Value);

        if (!ShowCreditDialog(dialogViewModel))
        {
            return null;
        }

        return new CreditDialogData(
            dialogViewModel.CreditId,
            dialogViewModel.Date,
            dialogViewModel.Type,
            dialogViewModel.Value);
    }

    private bool ShowDeleteCreditDialog()
    {
        if (SelectedCredit == null)
        {
            return false;
        }

        var dialogViewModel = CreditDialogViewModel.CreateForDelete(
            BrokerName,
            PortfolioName,
            AssetName,
            SelectedCredit.Id,
            SelectedCredit.Date,
            SelectedCredit.Type,
            SelectedCredit.Value);

        return ShowCreditDialog(dialogViewModel);
    }

}


