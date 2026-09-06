using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects an Investment Account's Name, IsActive, IsLiability, Source and (when Source is
/// CreditCard) linked credit card for both Create and Edit, mirroring
/// <see cref="IncomeSourceFormDialogViewModel"/>'s shape: validates shape only (that a name was
/// typed, and that a credit-card source has a card chosen), and lets the domain's refusal (e.g. a
/// duplicate name, or an inactive card) surface as a save error on the owning list ViewModel
/// rather than being re-decided here.
/// </summary>
public sealed class InvestmentAccountFormDialogViewModel : ViewModelBase
{
    private string _name;
    private bool _isActive;
    private bool _isLiability;
    private string _source;
    private Guid? _creditCardId;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    public string Title => IsEditing ? "Edit Investment Account" : "Create Investment Account";

    public IReadOnlyList<SourceOption> SourceOptions { get; } =
    [
        new("None", "None"),
        new("CreditCard", "Credit Card"),
        new("ReserveBucketsSum", "Sum of Reserve Buckets"),
    ];

    public IReadOnlyList<CreditCardDTO> CreditCardOptions { get; }

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

    public string Source
    {
        get => _source;
        set
        {
            if (SetProperty(ref _source, value))
            {
                OnPropertyChanged(nameof(IsCreditCardSourceMode));
                OnPropertyChanged(nameof(IsReserveBucketsSourceMode));
                Validate();
            }
        }
    }

    public Guid? CreditCardId
    {
        get => _creditCardId;
        set
        {
            if (SetProperty(ref _creditCardId, value))
            {
                Validate();
            }
        }
    }

    public bool IsCreditCardSourceMode => Source == "CreditCard";

    public bool IsReserveBucketsSourceMode => Source == "ReserveBucketsSum";

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public InvestmentAccountFormDialogViewModel(
        IReadOnlyList<CreditCardDTO> creditCardOptions,
        string? currentName = null,
        bool currentIsActive = true,
        bool currentIsLiability = false,
        string currentSource = "None",
        Guid? currentCreditCardId = null)
    {
        IsEditing = currentName is not null;
        CreditCardOptions = creditCardOptions ?? throw new ArgumentNullException(nameof(creditCardOptions));
        _name = currentName ?? string.Empty;
        _isActive = currentIsActive;
        _isLiability = currentIsLiability;
        _source = currentSource;
        _creditCardId = currentCreditCardId;

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
        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationMessage = "Name is required.";
        }
        else if (IsCreditCardSourceMode && CreditCardId is null)
        {
            ValidationMessage = "Select a credit card.";
        }
        else
        {
            ValidationMessage = string.Empty;
        }

        ConfirmCommand.RaiseCanExecuteChanged();
    }
}

/// <summary>A Source ComboBox entry: <see cref="Value"/> is the wire value stored on
/// <see cref="InvestmentAccountFormDialogViewModel.Source"/>; <see cref="Display"/> is the
/// human-readable label shown in the picker (matching Financial.Web's dropdown text).</summary>
public sealed record SourceOption(string Value, string Display);
