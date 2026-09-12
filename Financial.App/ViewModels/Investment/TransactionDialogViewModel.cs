using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;

namespace Financial.Presentation.App.ViewModels.Investment;

public enum TransactionDialogMode
{
    Add,
    Update,
    Delete
}

public sealed class TransactionDialogViewModel : ViewModelBase
{
    private DateTime _date;
    private string _type = string.Empty;
    private decimal _quantity;
    private decimal _unitPrice;
    private decimal _fees;
    private decimal _withheld;
    private string _validationMessage = string.Empty;

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
                Validate();
                OnPropertyChanged(nameof(HasQuantityEffect));
                OnPropertyChanged(nameof(NetCash));
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
                OnPropertyChanged(nameof(NetCash));
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
        decimal withheld)
    {
        Mode = mode;
        BrokerName = brokerName;
        PortfolioName = portfolioName;
        AssetName = assetName;
        TransactionId = transactionId;

        _date = date;
        _type = type;
        _quantity = quantity;
        _unitPrice = unitPrice;
        _fees = fees;
        _withheld = withheld;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    public static TransactionDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName) =>
        CreateForAdd(brokerName, portfolioName, assetName, DateTime.Today, "Buy");

    public static TransactionDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName, DateTime date, string type)
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
            0);
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

        return string.IsNullOrWhiteSpace(ValidationMessage);
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
}
