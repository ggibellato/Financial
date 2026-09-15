using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.ViewModels.Admin;

/// <summary>
/// Collects a Tax Rule's Jurisdiction, EventCategory (both locked once editing - TaxRuleUpdateDTO
/// has no such fields, per F01), Label, Description and effective date range for both Create and
/// Edit, mirroring <see cref="ReserveBucketFormDialogViewModel"/>'s shape: validates the same shape
/// the server does (a label was typed, EffectiveFrom is set and precedes EffectiveTo when both are
/// present) and lets the server's own refusal (e.g. an overlapping range, or a blocked delete) surface
/// as a save error on the owning list ViewModel rather than being re-decided here.
/// </summary>
public sealed class TaxRuleFormDialogViewModel : ViewModelBase
{
    private Jurisdiction _jurisdiction;
    private EventCategory _eventCategory;
    private string _label;
    private string _description;
    private DateOnly? _effectiveFrom;
    private DateOnly? _effectiveTo;
    private string _validationMessage = string.Empty;

    public bool IsEditing { get; }

    public bool IsCreateMode => !IsEditing;

    public string Title => IsEditing ? "Edit Tax Rule" : "Create Tax Rule";

    public IReadOnlyList<Jurisdiction> Jurisdictions { get; } = [Jurisdiction.BR, Jurisdiction.UK];

    public IReadOnlyList<EventCategory> EventCategories { get; } =
        [EventCategory.CapitalGain, EventCategory.Dividend, EventCategory.Interest, EventCategory.SecuritiesLendingIncome];

    public Jurisdiction Jurisdiction
    {
        get => _jurisdiction;
        set => SetProperty(ref _jurisdiction, value);
    }

    public EventCategory EventCategory
    {
        get => _eventCategory;
        set => SetProperty(ref _eventCategory, value);
    }

    public string Label
    {
        get => _label;
        set
        {
            if (SetProperty(ref _label, value))
            {
                Validate();
            }
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public DateOnly? EffectiveFrom
    {
        get => _effectiveFrom;
        set
        {
            if (SetProperty(ref _effectiveFrom, value))
            {
                Validate();
            }
        }
    }

    public DateOnly? EffectiveTo
    {
        get => _effectiveTo;
        set
        {
            if (SetProperty(ref _effectiveTo, value))
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

    public TaxRuleFormDialogViewModel(TaxRuleDTO? currentRule = null)
    {
        IsEditing = currentRule is not null;
        _jurisdiction = currentRule?.Jurisdiction ?? Jurisdiction.BR;
        _eventCategory = currentRule?.EventCategory ?? EventCategory.CapitalGain;
        _label = currentRule?.Label ?? string.Empty;
        _description = currentRule?.Description ?? string.Empty;
        _effectiveFrom = currentRule?.EffectiveFrom;
        _effectiveTo = currentRule?.EffectiveTo;

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

        Label = Label.Trim();
        CloseRequested?.Invoke(this, true);
    }

    private void Cancel() => CloseRequested?.Invoke(this, false);

    private bool CanConfirm() => string.IsNullOrWhiteSpace(ValidationMessage);

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            ValidationMessage = "Label is required.";
        }
        else if (EffectiveFrom is null)
        {
            ValidationMessage = "Effective From is required.";
        }
        else if (EffectiveTo.HasValue && EffectiveFrom >= EffectiveTo)
        {
            ValidationMessage = "Effective From must be before Effective To.";
        }
        else
        {
            ValidationMessage = string.Empty;
        }

        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
