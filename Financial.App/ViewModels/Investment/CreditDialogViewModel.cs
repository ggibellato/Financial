namespace Financial.Presentation.App.ViewModels.Investment;

public enum CreditDialogMode
{
    Add,
    Update,
    Delete
}

public sealed class CreditDialogViewModel : ViewModelBase
{
    private DateTime _date;
    private string _type = string.Empty;
    private decimal _value;
    private decimal _withheld;
    private string _validationMessage = string.Empty;

    public CreditDialogMode Mode { get; }
    public Guid CreditId { get; }
    public string BrokerName { get; }
    public string PortfolioName { get; }
    public string AssetName { get; }

    public string Title => Mode switch
    {
        CreditDialogMode.Add => "New credit",
        CreditDialogMode.Update => "Edit credit",
        CreditDialogMode.Delete => "Delete Credit",
        _ => "Credit"
    };

    public string ConfirmLabel => Mode switch
    {
        CreditDialogMode.Add => "Add credit",
        CreditDialogMode.Update => "Save",
        CreditDialogMode.Delete => "Delete",
        _ => "Confirm"
    };

    public bool IsReadOnly => Mode == CreditDialogMode.Delete;
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

    public decimal Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                Validate();
                OnPropertyChanged(nameof(NetAmount));
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
                OnPropertyChanged(nameof(NetAmount));
            }
        }
    }

    /// <summary>Live preview mirroring <see cref="Financial.Investment.Domain.Entities.Credit.NetAmount"/>'s
    /// formula exactly so the number shown here never drifts from what the server computes and persists.</summary>
    public decimal NetAmount => Value - Withheld;

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }
    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public CreditDialogViewModel(
        CreditDialogMode mode,
        string brokerName,
        string portfolioName,
        string assetName,
        Guid creditId,
        DateTime date,
        string type,
        decimal value,
        decimal withheld)
    {
        Mode = mode;
        BrokerName = brokerName;
        PortfolioName = portfolioName;
        AssetName = assetName;
        CreditId = creditId;

        _date = date;
        _type = type;
        _value = value;
        _withheld = withheld;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    public static CreditDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName) =>
        CreateForAdd(brokerName, portfolioName, assetName, DateTime.Today, "Dividend");

    public static CreditDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName, DateTime date, string type)
    {
        return new CreditDialogViewModel(
            CreditDialogMode.Add,
            brokerName,
            portfolioName,
            assetName,
            Guid.Empty,
            date,
            type,
            0,
            0);
    }

    public static CreditDialogViewModel CreateForUpdate(string brokerName, string portfolioName, string assetName, Guid id, DateTime date, string type, decimal value, decimal withheld)
    {
        return new CreditDialogViewModel(
            CreditDialogMode.Update,
            brokerName,
            portfolioName,
            assetName,
            id,
            date,
            type,
            value,
            withheld);
    }

    public static CreditDialogViewModel CreateForDelete(string brokerName, string portfolioName, string assetName, Guid id, DateTime date, string type, decimal value, decimal withheld)
    {
        return new CreditDialogViewModel(
            CreditDialogMode.Delete,
            brokerName,
            portfolioName,
            assetName,
            id,
            date,
            type,
            value,
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

    private bool CanConfirm()
    {
        if (Mode == CreditDialogMode.Delete)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(ValidationMessage);
    }

    private void Validate()
    {
        ValidationMessage = CreditDialogValidation.BuildValidationMessage(
            Mode == CreditDialogMode.Delete,
            Date,
            Type,
            Value,
            Withheld);
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
