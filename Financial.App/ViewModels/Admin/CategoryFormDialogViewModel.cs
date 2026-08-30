namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects a Category's Name, Active, IsInvestment and IsTithe for both Create and Edit, mirroring
/// <see cref="BankFormDialogViewModel"/>'s shape: validates shape only (that a name was typed), and
/// lets the domain's refusal (e.g. a duplicate name) surface as a save error on the owning list
/// ViewModel rather than being re-decided here.
/// </summary>
public sealed class CategoryFormDialogViewModel : ViewModelBase
{
    private string _name;
    private bool _active;
    private bool _isInvestment;
    private bool _isTithe;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    public string Title => IsEditing ? "Edit Category" : "Create Category";

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

    public bool Active
    {
        get => _active;
        set => SetProperty(ref _active, value);
    }

    public bool IsInvestment
    {
        get => _isInvestment;
        set => SetProperty(ref _isInvestment, value);
    }

    public bool IsTithe
    {
        get => _isTithe;
        set => SetProperty(ref _isTithe, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public CategoryFormDialogViewModel(
        string? currentName = null,
        bool currentActive = true,
        bool currentIsInvestment = false,
        bool currentIsTithe = false)
    {
        IsEditing = currentName is not null;
        _name = currentName ?? string.Empty;
        _active = currentActive;
        _isInvestment = currentIsInvestment;
        _isTithe = currentIsTithe;

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
