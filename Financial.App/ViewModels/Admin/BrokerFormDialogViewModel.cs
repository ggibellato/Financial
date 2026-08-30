namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects a Broker's Name and Currency for both Create and Edit, mirroring
/// <see cref="Investment.MoveAssetDialogViewModel"/>'s shape: validates shape only (that a name was
/// typed), and lets the domain's refusal (e.g. a duplicate name) surface as a save error on the
/// owning list ViewModel rather than being re-decided here.
/// </summary>
public sealed class BrokerFormDialogViewModel : ViewModelBase
{
    private string _name;
    private string _currency;
    private string _validationMessage = string.Empty;

    /// <summary>The Investment bounded context has no shared currency enum (CashFlow's Currency is
    /// BRL/GBP only and out of reach across the bounded-context boundary); these are the values
    /// already observed in this codebase's broker fixtures - matches BrokerFormDialog.tsx.</summary>
    public static readonly string[] Currencies = ["BRL", "GBP", "USD"];

    public bool IsEditing { get; }

    public string Title => IsEditing ? "Edit Broker" : "Create Broker";

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                Validate();
            }
        }
    }

    public string Currency
    {
        get => _currency;
        set => SetProperty(ref _currency, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public BrokerFormDialogViewModel(string? currentName = null, string? currentCurrency = null)
    {
        IsEditing = currentName is not null;
        _name = currentName ?? string.Empty;
        _currency = currentCurrency ?? Currencies[0];

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    private void Confirm()
    {
        Validate();
        if (!CanConfirm())
        {
            return;
        }

        Name = Name.Trim();
        CloseRequested?.Invoke(this, true);
    }

    private void Cancel() => CloseRequested?.Invoke(this, false);

    private bool CanConfirm() => string.IsNullOrWhiteSpace(ValidationMessage);

    private void Validate()
    {
        ValidationMessage = string.IsNullOrWhiteSpace(Name) ? "Name is required." : string.Empty;
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
