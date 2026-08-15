namespace Financial.Presentation.App.ViewModels.Investment;

public enum PriceDialogMode
{
    Add,
    Update,
    Delete
}

public sealed class PriceDialogViewModel : ViewModelBase
{
    private DateTime _date;
    private decimal _price;
    private string _validationMessage = string.Empty;

    public PriceDialogMode Mode { get; }
    public string BrokerName { get; }
    public string PortfolioName { get; }
    public string AssetName { get; }

    public string Title => Mode switch
    {
        PriceDialogMode.Add => "Add Price",
        PriceDialogMode.Update => "Update Price",
        PriceDialogMode.Delete => "Delete Price",
        _ => "Price"
    };

    public string ConfirmLabel => Mode switch
    {
        PriceDialogMode.Add => "Add",
        PriceDialogMode.Update => "Update",
        PriceDialogMode.Delete => "Delete",
        _ => "Confirm"
    };

    public bool IsReadOnly => Mode == PriceDialogMode.Delete;
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

    public decimal Price
    {
        get => _price;
        set
        {
            if (SetProperty(ref _price, value))
            {
                Validate();
            }
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

    public PriceDialogViewModel(
        PriceDialogMode mode,
        string brokerName,
        string portfolioName,
        string assetName,
        DateTime date,
        decimal price)
    {
        Mode = mode;
        BrokerName = brokerName;
        PortfolioName = portfolioName;
        AssetName = assetName;

        _date = date;
        _price = price;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    public static PriceDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName) =>
        new(PriceDialogMode.Add, brokerName, portfolioName, assetName, DateTime.Today, 0);

    public static PriceDialogViewModel CreateForUpdate(string brokerName, string portfolioName, string assetName, DateTime date, decimal price) =>
        new(PriceDialogMode.Update, brokerName, portfolioName, assetName, date, price);

    public static PriceDialogViewModel CreateForDelete(string brokerName, string portfolioName, string assetName, DateTime date, decimal price) =>
        new(PriceDialogMode.Delete, brokerName, portfolioName, assetName, date, price);

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
        if (Mode == PriceDialogMode.Delete)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(ValidationMessage);
    }

    private void Validate()
    {
        ValidationMessage = PriceDialogValidation.BuildValidationMessage(Mode == PriceDialogMode.Delete, Date, Price);
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
