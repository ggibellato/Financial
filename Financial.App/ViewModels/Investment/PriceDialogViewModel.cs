using Financial.Shared.Abstractions.Time;
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
        PriceDialogMode.Add => "New price",
        PriceDialogMode.Update => "Edit price",
        PriceDialogMode.Delete => "Delete Price",
        _ => "Price"
    };

    public string ConfirmLabel => Mode switch
    {
        PriceDialogMode.Add => "Add price",
        PriceDialogMode.Update => "Save",
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

    private readonly TimeProvider _timeProvider;

    public PriceDialogViewModel(
        PriceDialogMode mode,
        string brokerName,
        string portfolioName,
        string assetName,
        DateTime date,
        decimal price,
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
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

    public static PriceDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName, TimeProvider timeProvider) =>
        CreateForAdd(brokerName, portfolioName, assetName, timeProvider.GetLocalDate(), timeProvider);

    public static PriceDialogViewModel CreateForAdd(string brokerName, string portfolioName, string assetName, DateTime date, TimeProvider timeProvider) =>
        new(PriceDialogMode.Add, brokerName, portfolioName, assetName, date, 0, timeProvider);

    public static PriceDialogViewModel CreateForUpdate(string brokerName, string portfolioName, string assetName, DateTime date, decimal price, TimeProvider timeProvider) =>
        new(PriceDialogMode.Update, brokerName, portfolioName, assetName, date, price, timeProvider);

    public static PriceDialogViewModel CreateForDelete(string brokerName, string portfolioName, string assetName, DateTime date, decimal price, TimeProvider timeProvider) =>
        new(PriceDialogMode.Delete, brokerName, portfolioName, assetName, date, price, timeProvider);

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
        ValidationMessage = PriceDialogValidation.BuildValidationMessage(Mode == PriceDialogMode.Delete, Date, Price, _timeProvider.GetLocalDate());
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
