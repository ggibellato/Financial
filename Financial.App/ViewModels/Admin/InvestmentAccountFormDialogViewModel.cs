namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects an Investment Account's Name, IsActive and IsLiability for both Create and Edit,
/// mirroring <see cref="IncomeSourceFormDialogViewModel"/>'s shape: validates shape only (that a
/// name was typed), and lets the domain's refusal (e.g. a duplicate name) surface as a save error
/// on the owning list ViewModel rather than being re-decided here.
/// </summary>
public sealed class InvestmentAccountFormDialogViewModel : ViewModelBase
{
    private string _name;
    private bool _isActive;
    private bool _isLiability;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    public string Title => IsEditing ? "Edit Investment Account" : "Create Investment Account";

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

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool IsLiability
    {
        get => _isLiability;
        set => SetProperty(ref _isLiability, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public InvestmentAccountFormDialogViewModel(
        string? currentName = null,
        bool currentIsActive = true,
        bool currentIsLiability = false)
    {
        IsEditing = currentName is not null;
        _name = currentName ?? string.Empty;
        _isActive = currentIsActive;
        _isLiability = currentIsLiability;

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
