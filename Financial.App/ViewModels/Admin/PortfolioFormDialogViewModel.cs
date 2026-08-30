namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects a Portfolio's parent Broker (create only, fixed on edit) and Name, mirroring
/// <see cref="BrokerFormDialogViewModel"/>'s shape: validates shape only, and lets the domain's
/// refusal (e.g. a duplicate name) surface as a save error on the owning list ViewModel rather than
/// being re-decided here.
/// </summary>
public sealed class PortfolioFormDialogViewModel : ViewModelBase
{
    private string _name;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    /// <summary>The Broker picker's enabled state — bindable directly to IsEnabled, no converter needed.</summary>
    public bool CanChangeBroker => !IsEditing;

    public string Title => IsEditing ? "Edit Portfolio" : "Create Portfolio";

    private string _brokerName;

    /// <summary>Bindable so the create picker can select it; the view disables the control entirely
    /// when <see cref="IsEditing"/>, so it never actually changes after construction on an edit.</summary>
    public string BrokerName
    {
        get => _brokerName;
        set => SetProperty(ref _brokerName, value);
    }

    public IReadOnlyList<string> ActiveBrokerNames { get; }

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

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    /// <param name="activeBrokerNames">Brokers offered in the create picker. Ignored when editing.</param>
    /// <param name="currentBrokerName">Set only when editing; fixes <see cref="BrokerName"/>.</param>
    public PortfolioFormDialogViewModel(IReadOnlyList<string> activeBrokerNames, string? currentBrokerName = null, string? currentName = null)
    {
        IsEditing = currentName is not null;
        // A disabled ComboBox still needs its SelectedValue present in ItemsSource to display it,
        // so editing shows a single-item list holding just the fixed broker rather than the reused
        // Active-only list, which may not even contain a Historic portfolio's broker.
        ActiveBrokerNames = IsEditing && currentBrokerName is not null ? [currentBrokerName] : activeBrokerNames;
        _brokerName = currentBrokerName ?? activeBrokerNames.FirstOrDefault() ?? string.Empty;
        _name = currentName ?? string.Empty;

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
        ValidationMessage = string.IsNullOrWhiteSpace(Name)
            ? "Name is required."
            : !IsEditing && string.IsNullOrWhiteSpace(BrokerName)
                ? "A broker is required."
                : string.Empty;
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
