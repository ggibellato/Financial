using System;
using Financial.Presentation.Shared.ViewModels;

namespace Financial.Presentation.UI.ViewModels;

public enum OperationDialogMode
{
    Add,
    Update,
    Delete
}

public sealed class OperationDialogViewModel : ViewModelBase
{
    private DateTime _date;
    private string _type = string.Empty;
    private decimal _quantity;
    private decimal _unitPrice;
    private decimal _fees;
    private string _validationMessage = string.Empty;

    public OperationDialogMode Mode { get; }
    public Guid OperationId { get; }
    public string BrokerName { get; }
    public string PortfolioName { get; }
    public string AssetName { get; }

    public string Title => Mode switch
    {
        OperationDialogMode.Add => "Add Operation",
        OperationDialogMode.Update => "Update Operation",
        OperationDialogMode.Delete => "Delete Operation",
        _ => "Operation"
    };

    public string ConfirmLabel => Mode switch
    {
        OperationDialogMode.Add => "Add",
        OperationDialogMode.Update => "Update",
        OperationDialogMode.Delete => "Delete",
        _ => "Confirm"
    };

    public bool IsReadOnly => Mode == OperationDialogMode.Delete;
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
                Validate();
            }
        }
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                Validate();
                OnPropertyChanged(nameof(TotalPrice));
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
                OnPropertyChanged(nameof(TotalPrice));
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
                OnPropertyChanged(nameof(TotalPrice));
            }
        }
    }

    public decimal TotalPrice => UnitPrice * Quantity + Fees;

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }
    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public OperationDialogViewModel(
        OperationDialogMode mode,
        string brokerName,
        string portfolioName,
        string assetName,
        Guid operationId,
        DateTime date,
        string type,
        decimal quantity,
        decimal unitPrice,
        decimal fees)
    {
        Mode = mode;
        BrokerName = brokerName;
        PortfolioName = portfolioName;
        AssetName = assetName;
        OperationId = operationId;

        _date = date;
        _type = type;
        _quantity = quantity;
        _unitPrice = unitPrice;
        _fees = fees;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    public static OperationDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName)
    {
        return new OperationDialogViewModel(
            OperationDialogMode.Add,
            brokerName,
            portfolioName,
            assetName,
            Guid.Empty,
            DateTime.Today,
            "Buy",
            0,
            0,
            0);
    }

    public static OperationDialogViewModel CreateForUpdate(string brokerName, string portfolioName, string assetName, Guid id, DateTime date, string type, decimal quantity, decimal unitPrice, decimal fees)
    {
        return new OperationDialogViewModel(
            OperationDialogMode.Update,
            brokerName,
            portfolioName,
            assetName,
            id,
            date,
            type,
            quantity,
            unitPrice,
            fees);
    }

    public static OperationDialogViewModel CreateForDelete(string brokerName, string portfolioName, string assetName, Guid id, DateTime date, string type, decimal quantity, decimal unitPrice, decimal fees)
    {
        return new OperationDialogViewModel(
            OperationDialogMode.Delete,
            brokerName,
            portfolioName,
            assetName,
            id,
            date,
            type,
            quantity,
            unitPrice,
            fees);
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

    private bool CanConfirm()
    {
        if (Mode == OperationDialogMode.Delete)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(ValidationMessage);
    }

    private void Validate()
    {
        ValidationMessage = OperationDialogValidation.BuildValidationMessage(
            Mode == OperationDialogMode.Delete,
            Date,
            Type,
            Quantity,
            UnitPrice,
            Fees);
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
