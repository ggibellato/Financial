namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects a Bank's Name and RoundUpEnabled for both Create and Edit, mirroring
/// <see cref="BrokerFormDialogViewModel"/>'s shape: validates shape only (that a name was typed), and
/// lets the domain's refusal (e.g. a duplicate name) surface as a save error on the owning list
/// ViewModel rather than being re-decided here.
/// </summary>
public sealed class BankFormDialogViewModel : ViewModelBase
{
    private string _name;
    private bool _roundUpEnabled;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    public string Title => IsEditing ? "Edit Bank" : "Create Bank";

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

    public bool RoundUpEnabled
    {
        get => _roundUpEnabled;
        set => SetProperty(ref _roundUpEnabled, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public BankFormDialogViewModel(string? currentName = null, bool currentRoundUpEnabled = false)
    {
        IsEditing = currentName is not null;
        _name = currentName ?? string.Empty;
        _roundUpEnabled = currentRoundUpEnabled;

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
