namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects a Reserve Bucket's Name, SplitPercentage and IsActive for both Create and Edit,
/// mirroring <see cref="IncomeSourceFormDialogViewModel"/>'s shape: validates shape only (that a
/// name was typed and the split percentage is a number between 0 and 100, the same guard the
/// domain's own Update/Create enforce), and lets the domain's refusal (e.g. a duplicate name)
/// surface as a save error on the owning list ViewModel rather than being re-decided here.
/// </summary>
public sealed class ReserveBucketFormDialogViewModel : ViewModelBase
{
    private string _name;
    private string _splitPercentage;
    private bool _isActive;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    public string Title => IsEditing ? "Edit Reserve Bucket" : "Create Reserve Bucket";

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

    public string SplitPercentage
    {
        get => _splitPercentage;
        set
        {
            if (SetProperty(ref _splitPercentage, value))
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

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public ReserveBucketFormDialogViewModel(
        string? currentName = null,
        decimal currentSplitPercentage = 0m,
        bool currentIsActive = true)
    {
        IsEditing = currentName is not null;
        _name = currentName ?? string.Empty;
        _splitPercentage = IsEditing ? currentSplitPercentage.ToString() : string.Empty;
        _isActive = currentIsActive;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    public decimal ParsedSplitPercentage => decimal.TryParse(SplitPercentage, out var value) ? value : 0m;

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
        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationMessage = "Name is required.";
        }
        else if (!decimal.TryParse(SplitPercentage, out var parsed) || parsed < 0 || parsed > 100)
        {
            ValidationMessage = "Split percentage must be between 0 and 100.";
        }
        else
        {
            ValidationMessage = string.Empty;
        }

        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
