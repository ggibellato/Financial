namespace Financial.Presentation.App.ViewModels.Investment;

public enum CorporateActionFormMode
{
    Add,
    Update,
    Delete
}

public sealed class CorporateActionFormViewModel : ViewModelBase
{
    private DateTime _effectiveDate;
    private string _type = CorporateActionFormValidation.SplitTypeValue;
    private decimal _ratioNumerator;
    private decimal _ratioDenominator;
    private string _note = string.Empty;
    private string _validationMessage = string.Empty;

    public CorporateActionFormMode Mode { get; }
    public Guid CorporateActionId { get; }
    public string BrokerName { get; }
    public string PortfolioName { get; }
    public string AssetName { get; }

    public string Title => Mode switch
    {
        CorporateActionFormMode.Add => "New corporate action",
        CorporateActionFormMode.Update => "Edit corporate action",
        CorporateActionFormMode.Delete => "Delete Corporate Action",
        _ => "Corporate Action"
    };

    public string ConfirmLabel => Mode switch
    {
        CorporateActionFormMode.Add => "Add corporate action",
        CorporateActionFormMode.Update => "Save",
        CorporateActionFormMode.Delete => "Delete",
        _ => "Confirm"
    };

    public bool IsReadOnly => Mode == CorporateActionFormMode.Delete;
    public bool IsEditable => !IsReadOnly;

    public DateTime EffectiveDate
    {
        get => _effectiveDate;
        set
        {
            if (SetProperty(ref _effectiveDate, value))
            {
                Validate();
            }
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
            {
                Validate();
            }
        }
    }

    public decimal RatioNumerator
    {
        get => _ratioNumerator;
        set
        {
            if (SetProperty(ref _ratioNumerator, value))
            {
                Validate();
            }
        }
    }

    public decimal RatioDenominator
    {
        get => _ratioDenominator;
        set
        {
            if (SetProperty(ref _ratioDenominator, value))
            {
                Validate();
            }
        }
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand ConfirmCommand { get; }
    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public CorporateActionFormViewModel(
        CorporateActionFormMode mode,
        string brokerName,
        string portfolioName,
        string assetName,
        Guid corporateActionId,
        DateTime effectiveDate,
        string type,
        decimal ratioNumerator,
        decimal ratioDenominator,
        string? note)
    {
        Mode = mode;
        BrokerName = brokerName;
        PortfolioName = portfolioName;
        AssetName = assetName;
        CorporateActionId = corporateActionId;

        _effectiveDate = effectiveDate;
        _type = type;
        _ratioNumerator = ratioNumerator;
        _ratioDenominator = ratioDenominator;
        _note = note ?? string.Empty;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        Validate();
    }

    public static CorporateActionFormViewModel CreateForAdd(string brokerName, string portfolioName, string assetName) =>
        new(CorporateActionFormMode.Add, brokerName, portfolioName, assetName, Guid.Empty, DateTime.Today,
            CorporateActionFormValidation.SplitTypeValue, 0m, 0m, null);

    public static CorporateActionFormViewModel CreateForUpdate(
        string brokerName,
        string portfolioName,
        string assetName,
        Guid id,
        DateTime effectiveDate,
        string type,
        decimal ratioNumerator,
        decimal ratioDenominator,
        string? note) =>
        new(CorporateActionFormMode.Update, brokerName, portfolioName, assetName, id, effectiveDate, type, ratioNumerator, ratioDenominator, note);

    public static CorporateActionFormViewModel CreateForDelete(
        string brokerName,
        string portfolioName,
        string assetName,
        Guid id,
        DateTime effectiveDate,
        string type,
        decimal ratioNumerator,
        decimal ratioDenominator,
        string? note) =>
        new(CorporateActionFormMode.Delete, brokerName, portfolioName, assetName, id, effectiveDate, type, ratioNumerator, ratioDenominator, note);

    private void Confirm()
    {
        Validate();
        if (!CanConfirm())
        {
            return;
        }

        CloseRequested?.Invoke(this, true);
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    /// <summary>Surfaces a server-side refusal inline, in the same slot as client-side validation.
    /// Reusing <see cref="ValidationMessage"/> also disables Confirm until the user changes a field,
    /// since resubmitting the same values would fail identically.</summary>
    public void ReportSubmitFailed(string message)
    {
        ValidationMessage = message;
        ConfirmCommand.RaiseCanExecuteChanged();
    }

    private bool CanConfirm()
    {
        if (Mode == CorporateActionFormMode.Delete)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(ValidationMessage);
    }

    private void Validate()
    {
        ValidationMessage = CorporateActionFormValidation.BuildValidationMessage(
            Mode == CorporateActionFormMode.Delete,
            EffectiveDate,
            RatioNumerator,
            RatioDenominator);
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
