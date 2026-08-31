namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects a Recurring Bill's DueDay, Description, Value, Area, Note, NitNumber,
/// MinimumWageValue and (edit-only) Status, mirroring <see cref="IncomeSourceFormDialogViewModel"/>'s
/// shape: validates shape only (reusing <see cref="RecurringBillFormValidation"/>, the same rule the
/// domain's own <c>Validate</c> enforces), and lets the domain's refusal surface as a save error on
/// the owning list ViewModel rather than being re-decided here.
/// </summary>
public sealed class RecurringBillFormDialogViewModel : ViewModelBase
{
    private string _dueDay;
    private string _description;
    private string _value;
    private string _area;
    private string _note;
    private string _nitNumber;
    private string _minimumWageValue;
    private string _status;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    public string Title => IsEditing ? "Edit Recurring Bill" : "Create Recurring Bill";

    public IReadOnlyList<string> AreaOptions { get; } = ["Brasil", "UK"];

    public IReadOnlyList<string> StatusOptions { get; } = ["Unset", "Scheduled", "Paid"];

    public string DueDay
    {
        get => _dueDay;
        set
        {
            if (SetProperty(ref _dueDay, value))
            {
                Validate();
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                Validate();
            }
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                Validate();
            }
        }
    }

    public string Area
    {
        get => _area;
        set => SetProperty(ref _area, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public string NitNumber
    {
        get => _nitNumber;
        set => SetProperty(ref _nitNumber, value);
    }

    public string MinimumWageValue
    {
        get => _minimumWageValue;
        set => SetProperty(ref _minimumWageValue, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public RecurringBillFormDialogViewModel(
        Guid? currentId = null,
        int currentDueDay = 0,
        string currentDescription = "",
        decimal currentValue = 0m,
        string currentArea = "Brasil",
        string currentNote = "",
        string? currentNitNumber = null,
        decimal? currentMinimumWageValue = null,
        string currentStatus = "Unset")
    {
        IsEditing = currentId is not null;
        _dueDay = IsEditing ? currentDueDay.ToString() : string.Empty;
        _description = currentDescription;
        _value = IsEditing ? currentValue.ToString() : string.Empty;
        _area = currentArea;
        _note = currentNote;
        _nitNumber = currentNitNumber ?? string.Empty;
        _minimumWageValue = currentMinimumWageValue?.ToString() ?? string.Empty;
        _status = currentStatus;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    public int ParsedDueDay => int.TryParse(DueDay, out var value) ? value : 0;

    public decimal ParsedValue => decimal.TryParse(Value, out var value) ? value : 0m;

    public decimal? ParsedMinimumWageValue => decimal.TryParse(MinimumWageValue, out var value) ? value : null;

    public string? NormalizedNitNumber => string.IsNullOrWhiteSpace(NitNumber) ? null : NitNumber;

    private void Confirm()
    {
        Validate();
        if (!CanConfirm())
        {
            return;
        }

        Description = Description.Trim();
        CloseRequested?.Invoke(this, true);
    }

    private void Cancel() => CloseRequested?.Invoke(this, false);

    private bool CanConfirm() => string.IsNullOrWhiteSpace(ValidationMessage);

    private void Validate()
    {
        ValidationMessage = RecurringBillFormValidation.BuildValidationMessage(DueDay, Description, Value);
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
