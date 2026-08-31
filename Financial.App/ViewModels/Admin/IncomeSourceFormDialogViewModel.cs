namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects an Income Source's Name, Group, IsActive and AutoSplitToReserve for both Create and
/// Edit, mirroring <see cref="CategoryFormDialogViewModel"/>'s shape: validates shape only (that a
/// name was typed), and lets the domain's refusal (e.g. a duplicate name) surface as a save error
/// on the owning list ViewModel rather than being re-decided here.
/// </summary>
public sealed class IncomeSourceFormDialogViewModel : ViewModelBase
{
    private string _name;
    private string _group;
    private bool _isActive;
    private bool _autoSplitToReserve;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    public string Title => IsEditing ? "Edit Income Source" : "Create Income Source";

    public IReadOnlyList<string> GroupOptions { get; } = ["Salary", "DividendoJuros", "NonReportable"];

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

    public string Group
    {
        get => _group;
        set => SetProperty(ref _group, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool AutoSplitToReserve
    {
        get => _autoSplitToReserve;
        set => SetProperty(ref _autoSplitToReserve, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public IncomeSourceFormDialogViewModel(
        string? currentName = null,
        string currentGroup = "Salary",
        bool currentIsActive = true,
        bool currentAutoSplitToReserve = false)
    {
        IsEditing = currentName is not null;
        _name = currentName ?? string.Empty;
        _group = currentGroup;
        _isActive = currentIsActive;
        _autoSplitToReserve = currentAutoSplitToReserve;

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
