using System.Collections.ObjectModel;
using System.ComponentModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels.Investment;

public enum TransactionDialogMode
{
    Add,
    Update,
    Delete
}

public sealed class TransactionDialogViewModel : ViewModelBase
{
    private static readonly HashSet<string> LotAllocationTypes = new(StringComparer.OrdinalIgnoreCase) { "Sell", "Redemption" };

    private readonly bool _isSpecificIdBroker;
    private readonly Action? _fetchOpenLots;
    private bool _openLotsFetchStarted;

    private DateTime _date;
    private string _type = string.Empty;
    private decimal _quantity;
    private decimal _unitPrice;
    private decimal _fees;
    private decimal _withheld;
    private string _validationMessage = string.Empty;
    private bool _isLoadingOpenLots;
    private string? _openLotsError;

    public TransactionDialogMode Mode { get; }
    public Guid TransactionId { get; }
    public string BrokerName { get; }
    public string PortfolioName { get; }
    public string AssetName { get; }

    public string Title => Mode switch
    {
        TransactionDialogMode.Add => "New transaction",
        TransactionDialogMode.Update => "Edit transaction",
        TransactionDialogMode.Delete => "Delete Transaction",
        _ => "Transaction"
    };

    public string ConfirmLabel => Mode switch
    {
        TransactionDialogMode.Add => "Add transaction",
        TransactionDialogMode.Update => "Save",
        TransactionDialogMode.Delete => "Delete",
        _ => "Confirm"
    };

    public bool IsReadOnly => Mode == TransactionDialogMode.Delete;
    public bool IsEditable => !IsReadOnly;

    public DateTime Date
    {
        get => _date;
        set
        {
            if (SetProperty(ref _date, value))
            {
                Validate();
            }
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
            {
                OnPropertyChanged(nameof(HasQuantityEffect));
                OnPropertyChanged(nameof(NetCash));
                OnPropertyChanged(nameof(RequiresLotAllocation));
                OnPropertyChanged(nameof(IsAllocationExact));
                if (RequiresLotAllocation) EnsureOpenLotsRequested();
                Validate();
            }
        }
    }

    /// <summary>True only when entering (never editing) a Sell/Redemption against a SpecificId
    /// broker - matches F04's Decision 6: lot allocation is a creation-time-only control.</summary>
    public bool RequiresLotAllocation =>
        Mode == TransactionDialogMode.Add && _isSpecificIdBroker && LotAllocationTypes.Contains(Type);

    public ObservableCollection<LotAllocationRowViewModel> OpenLots { get; } = new();

    public bool IsLoadingOpenLots
    {
        get => _isLoadingOpenLots;
        private set
        {
            if (SetProperty(ref _isLoadingOpenLots, value))
            {
                Validate();
            }
        }
    }

    public string? OpenLotsError
    {
        get => _openLotsError;
        private set
        {
            if (SetProperty(ref _openLotsError, value))
            {
                OnPropertyChanged(nameof(HasOpenLotsError));
                Validate();
            }
        }
    }

    public bool HasOpenLotsError => OpenLotsError != null;

    public decimal AllocatedTotal => LotAllocationCalculator.SumAllocations(OpenLots);
    public bool HasOverAllocatedLot => LotAllocationCalculator.HasOverAllocatedLot(OpenLots);
    public bool IsAllocationExact => LotAllocationCalculator.IsAllocationExact(AllocatedTotal, Quantity);
    public bool HasSaleQuantity => Quantity > 0;

    public RelayCommand RetryOpenLotsCommand { get; }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(NetCash));
                OnPropertyChanged(nameof(IsAllocationExact));
                OnPropertyChanged(nameof(HasSaleQuantity));
                Validate();
            }
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (SetProperty(ref _unitPrice, value))
            {
                Validate();
                OnPropertyChanged(nameof(NetCash));
            }
        }
    }

    public decimal Fees
    {
        get => _fees;
        set
        {
            if (SetProperty(ref _fees, value))
            {
                Validate();
                OnPropertyChanged(nameof(NetCash));
            }
        }
    }

    public decimal Withheld
    {
        get => _withheld;
        set
        {
            if (SetProperty(ref _withheld, value))
            {
                Validate();
                OnPropertyChanged(nameof(NetCash));
            }
        }
    }

    /// <summary>Whether the selected type has a quantity effect - when false, Quantity/UnitPrice
    /// are not required (FR-022) and the view hides them.</summary>
    public bool HasQuantityEffect =>
        !TransactionTypeParser.TryParse(Type, out var parsed) || TransactionTypeEffects.For(parsed).Quantity != QuantityEffect.None;

    /// <summary>Live preview of the net cash this transaction would move, mirroring
    /// <see cref="Transaction.NetCash"/>'s formula exactly so the number shown here never drifts
    /// from what the server will compute and persist.</summary>
    public decimal NetCash
    {
        get
        {
            if (!TransactionTypeParser.TryParse(Type, out var parsed))
            {
                return 0m;
            }

            var gross = UnitPrice * Quantity;
            return TransactionTypeEffects.For(parsed).Cash switch
            {
                CashEffect.Out => -(gross + Fees + Withheld),
                CashEffect.In => gross - Fees - Withheld,
                _ => -Fees
            };
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }
    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public TransactionDialogViewModel(
        TransactionDialogMode mode,
        string brokerName,
        string portfolioName,
        string assetName,
        Guid transactionId,
        DateTime date,
        string type,
        decimal quantity,
        decimal unitPrice,
        decimal fees,
        decimal withheld,
        bool isSpecificIdBroker = false,
        Action? fetchOpenLots = null)
    {
        Mode = mode;
        BrokerName = brokerName;
        PortfolioName = portfolioName;
        AssetName = assetName;
        TransactionId = transactionId;
        _isSpecificIdBroker = isSpecificIdBroker;
        _fetchOpenLots = fetchOpenLots;

        _date = date;
        _type = type;
        _quantity = quantity;
        _unitPrice = unitPrice;
        _fees = fees;
        _withheld = withheld;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);
        RetryOpenLotsCommand = new RelayCommand(RetryOpenLots);

        if (RequiresLotAllocation) EnsureOpenLotsRequested();

        Validate();
    }

    public static TransactionDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName) =>
        CreateForAdd(brokerName, portfolioName, assetName, DateTime.Today, "Buy", false, null);

    public static TransactionDialogViewModel CreateForAdd(
        string brokerName,
        string portfolioName,
        string assetName,
        DateTime date,
        string type,
        bool isSpecificIdBroker = false,
        Action? fetchOpenLots = null)
    {
        return new TransactionDialogViewModel(
            TransactionDialogMode.Add,
            brokerName,
            portfolioName,
            assetName,
            Guid.Empty,
            date,
            type,
            0,
            0,
            0,
            0,
            isSpecificIdBroker,
            fetchOpenLots);
    }

    public static TransactionDialogViewModel CreateForUpdate(string brokerName, string portfolioName, string assetName, Guid id, DateTime date, string type, decimal quantity, decimal unitPrice, decimal fees, decimal withheld)
    {
        return new TransactionDialogViewModel(
            TransactionDialogMode.Update,
            brokerName,
            portfolioName,
            assetName,
            id,
            date,
            type,
            quantity,
            unitPrice,
            fees,
            withheld);
    }

    public static TransactionDialogViewModel CreateForDelete(string brokerName, string portfolioName, string assetName, Guid id, DateTime date, string type, decimal quantity, decimal unitPrice, decimal fees, decimal withheld)
    {
        return new TransactionDialogViewModel(
            TransactionDialogMode.Delete,
            brokerName,
            portfolioName,
            assetName,
            id,
            date,
            type,
            quantity,
            unitPrice,
            fees,
            withheld);
    }

    private void Confirm()
    {
        Validate();
        if (!CanConfirm())
        {
            return;
        }

        CloseRequested?.Invoke(this, true);
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    /// <summary>Surfaces a server-side refusal (e.g. an oversell) inline, in the same slot as
    /// client-side validation. Reusing <see cref="ValidationMessage"/> also disables Confirm until
    /// the user changes a field, since resubmitting the same values would fail identically.</summary>
    public void ReportSubmitFailed(string message)
    {
        ValidationMessage = message;
        ConfirmCommand.RaiseCanExecuteChanged();
    }

    private bool CanConfirm()
    {
        if (Mode == TransactionDialogMode.Delete)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(ValidationMessage))
        {
            return false;
        }

        if (!RequiresLotAllocation)
        {
            return true;
        }

        return !IsLoadingOpenLots && !HasOpenLotsError && IsAllocationExact && !HasOverAllocatedLot;
    }

    private void Validate()
    {
        ValidationMessage = TransactionDialogValidation.BuildValidationMessage(
            Mode == TransactionDialogMode.Delete,
            Date,
            Type,
            Quantity,
            UnitPrice,
            Fees,
            Withheld);
        ConfirmCommand.RaiseCanExecuteChanged();
    }

    private void EnsureOpenLotsRequested()
    {
        if (_openLotsFetchStarted)
        {
            return;
        }

        _openLotsFetchStarted = true;
        _fetchOpenLots?.Invoke();
    }

    private void RetryOpenLots() => _fetchOpenLots?.Invoke();

    public void SetOpenLotsLoading()
    {
        OpenLots.Clear();
        IsLoadingOpenLots = true;
        OpenLotsError = null;
    }

    public void SetOpenLots(IReadOnlyList<OpenLotDTO> lots)
    {
        foreach (var row in OpenLots)
        {
            row.PropertyChanged -= OnOpenLotRowChanged;
        }

        OpenLots.Clear();
        foreach (var lot in lots)
        {
            var row = new LotAllocationRowViewModel(lot);
            row.PropertyChanged += OnOpenLotRowChanged;
            OpenLots.Add(row);
        }

        IsLoadingOpenLots = false;
        OpenLotsError = null;
        RaiseAllocationChanged();
    }

    public void SetOpenLotsError(string message)
    {
        IsLoadingOpenLots = false;
        OpenLotsError = message;
    }

    private void OnOpenLotRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LotAllocationRowViewModel.Quantity))
        {
            RaiseAllocationChanged();
        }
    }

    private void RaiseAllocationChanged()
    {
        OnPropertyChanged(nameof(AllocatedTotal));
        OnPropertyChanged(nameof(HasOverAllocatedLot));
        OnPropertyChanged(nameof(IsAllocationExact));
        Validate();
    }
}
