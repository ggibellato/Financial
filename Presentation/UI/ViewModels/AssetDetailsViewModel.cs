using System;
using System.Collections.ObjectModel;
using System.Windows;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.UI;

namespace Financial.Presentation.UI.ViewModels;

/// <summary>
/// ViewModel for displaying asset details with operations and credits in tabs
/// </summary>
public class AssetDetailsViewModel : ViewModelBase
{
    private readonly IOperationService? _operationService;
    private readonly RelayCommand _addOperationCommand;
    private readonly RelayCommand _updateOperationCommand;
    private readonly RelayCommand _deleteOperationCommand;
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
    private OperationDTO? _selectedOperation;

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
        private set => SetProperty(ref _quantity, value);
    }

    public decimal AveragePrice
    {
        get => _averagePrice;
        private set => SetProperty(ref _averagePrice, value);
    }

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public decimal TotalBought
    {
        get => _totalBought;
        private set => SetProperty(ref _totalBought, value);
    }

    public decimal TotalSold
    {
        get => _totalSold;
        private set => SetProperty(ref _totalSold, value);
    }

    public decimal TotalCredits
    {
        get => _totalCredits;
        private set => SetProperty(ref _totalCredits, value);
    }

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

    public RelayCommand AddOperationCommand => _addOperationCommand;
    public RelayCommand UpdateOperationCommand => _updateOperationCommand;
    public RelayCommand DeleteOperationCommand => _deleteOperationCommand;

    public AssetDetailsViewModel(IOperationService? operationService = null)
    {
        _operationService = operationService;
        _addOperationCommand = new RelayCommand(AddOperation, CanEditOperations);
        _updateOperationCommand = new RelayCommand(UpdateOperation, CanUpdateOperation);
        _deleteOperationCommand = new RelayCommand(DeleteOperation, CanDeleteOperation);
    }

    /// <summary>
    /// Loads asset details from DTO
    /// </summary>
    public void LoadAssetDetails(AssetDetailsDTO details)
    {
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
        Operations.Clear();
        Credits.Clear();
        SelectedOperation = null;
        UpdateCommandStates();
    }

    private bool HasOperationContext =>
        _operationService != null &&
        !string.IsNullOrWhiteSpace(BrokerName) &&
        !string.IsNullOrWhiteSpace(PortfolioName) &&
        !string.IsNullOrWhiteSpace(AssetName);

    private bool CanEditOperations() => HasOperationContext;

    private bool CanUpdateOperation(object? parameter) =>
        HasOperationContext && (parameter is OperationDTO || SelectedOperation != null);

    private bool CanDeleteOperation(object? parameter) =>
        HasOperationContext && (parameter is OperationDTO || SelectedOperation != null);

    private void AddOperation()
    {
        if (!HasOperationContext)
        {
            MessageBox.Show("Select an asset before adding an operation.", "Operation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_operationService == null)
        {
            return;
        }

        var dialogViewModel = OperationDialogViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        if (!ShowOperationDialog(dialogViewModel))
        {
            return;
        }

        if (!TryNormalizeOperationType(dialogViewModel.Type, out var normalizedType))
        {
            MessageBox.Show("Operation type must be 'Buy' or 'Sell'.", "Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var updatedDetails = _operationService.AddOperation(new OperationCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = dialogViewModel.Date,
            Type = normalizedType,
            Quantity = dialogViewModel.Quantity,
            UnitPrice = dialogViewModel.UnitPrice,
            Fees = dialogViewModel.Fees
        });

        if (updatedDetails == null)
        {
            MessageBox.Show("Operation could not be added. Check the values and try again.", "Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoadAssetDetails(updatedDetails);
    }

    private void UpdateOperation(object? parameter)
    {
        if (parameter is OperationDTO operation)
        {
            SelectedOperation = operation;
        }

        if (_operationService == null || SelectedOperation == null)
        {
            return;
        }

        if (SelectedOperation.Id == Guid.Empty)
        {
            MessageBox.Show("Select a saved operation to update.", "Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
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
            return;
        }

        if (!TryNormalizeOperationType(dialogViewModel.Type, out var normalizedType))
        {
            MessageBox.Show("Operation type must be 'Buy' or 'Sell'.", "Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var updatedDetails = _operationService.UpdateOperation(new OperationUpdateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Id = dialogViewModel.OperationId,
            Date = dialogViewModel.Date,
            Type = normalizedType,
            Quantity = dialogViewModel.Quantity,
            UnitPrice = dialogViewModel.UnitPrice,
            Fees = dialogViewModel.Fees
        });

        if (updatedDetails == null)
        {
            MessageBox.Show("Operation could not be updated. Check the values and try again.", "Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoadAssetDetails(updatedDetails);
    }

    private void DeleteOperation(object? parameter)
    {
        if (parameter is OperationDTO operation)
        {
            SelectedOperation = operation;
        }

        if (SelectedOperation == null)
        {
            return;
        }

        if (_operationService == null)
        {
            return;
        }

        if (SelectedOperation.Id == Guid.Empty)
        {
            MessageBox.Show("Select a saved operation to delete.", "Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
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

        if (!ShowOperationDialog(dialogViewModel))
        {
            return;
        }

        var updatedDetails = _operationService.DeleteOperation(new OperationDeleteDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Id = SelectedOperation.Id
        });

        if (updatedDetails == null)
        {
            MessageBox.Show("Operation could not be deleted. Check the values and try again.", "Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoadAssetDetails(updatedDetails);
    }

    private void UpdateCommandStates()
    {
        _addOperationCommand.RaiseCanExecuteChanged();
        _updateOperationCommand.RaiseCanExecuteChanged();
        _deleteOperationCommand.RaiseCanExecuteChanged();
    }

    private bool ShowOperationDialog(OperationDialogViewModel viewModel)
    {
        var dialog = new OperationDialog(viewModel)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true;
    }

    private static bool TryNormalizeOperationType(string? value, out string normalized)
    {
        if (string.Equals(value, "Buy", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "Buy";
            return true;
        }

        if (string.Equals(value, "Sell", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "Sell";
            return true;
        }

        normalized = string.Empty;
        return false;
    }
}


