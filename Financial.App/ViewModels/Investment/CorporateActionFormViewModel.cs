using System.ComponentModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.ViewModels.Investment;

public enum CorporateActionFormMode
{
    Add,
    Update,
    Delete
}

public enum CorporateActionFormStep
{
    Fields,
    Confirm
}

public sealed class CorporateActionFormViewModel : ViewModelBase
{
    private DateTime _effectiveDate;
    private string _type = CorporateActionFormValidation.SplitTypeValue;
    private decimal _ratioNumerator;
    private decimal _ratioDenominator;
    private string _note = string.Empty;
    private string _validationMessage = string.Empty;
    private CorporateActionFormStep _step = CorporateActionFormStep.Fields;
    private decimal _exchangeRatio;
    private decimal? _cashInLieuAmount;
    private decimal _quantityReceived;
    private decimal _allocationPercentage;

    public CorporateActionFormMode Mode { get; }
    public Guid CorporateActionId { get; }
    public string BrokerName { get; }
    public string PortfolioName { get; }
    public string AssetName { get; }

    public TargetAssetPickerViewModel? TargetAssetPicker { get; }
    public string TargetAssetName { get; }
    public string SourceAssetName { get; }
    public decimal SourceQuantity { get; }
    public decimal SourceCostBasis { get; }

    public string Title => Mode switch
    {
        CorporateActionFormMode.Add => "New corporate action",
        CorporateActionFormMode.Update => "Edit corporate action",
        CorporateActionFormMode.Delete => "Delete Corporate Action",
        _ => "Corporate Action"
    };

    public string ConfirmLabel =>
        IsMergerFieldsStep ? "Continue" :
        IsMergerConfirmStep ? "Confirm & Save" :
        Mode switch
        {
            CorporateActionFormMode.Add => "Add corporate action",
            CorporateActionFormMode.Update => "Save",
            CorporateActionFormMode.Delete => "Delete",
            _ => "Confirm"
        };

    public bool IsReadOnly => Mode == CorporateActionFormMode.Delete;
    public bool IsEditable => !IsReadOnly;
    public bool IsAddMode => Mode == CorporateActionFormMode.Add;
    public bool IsUpdateMode => Mode == CorporateActionFormMode.Update;

    public bool IsSplit => Type == CorporateActionFormValidation.SplitTypeValue;
    public bool IsMerger => Type == CorporateActionFormValidation.MergerTypeValue;
    public bool IsSpinOff => Type == CorporateActionFormValidation.SpinOffTypeValue;
    public bool IsMergerOrSpinOff => IsMerger || IsSpinOff;

    public CorporateActionFormStep Step
    {
        get => _step;
        private set
        {
            if (SetProperty(ref _step, value))
            {
                OnPropertyChanged(nameof(IsMergerFieldsStep));
                OnPropertyChanged(nameof(IsMergerConfirmStep));
                OnPropertyChanged(nameof(ConfirmLabel));
                OnPropertyChanged(nameof(ConfirmSummary));
                OnPropertyChanged(nameof(ConfirmSummaryTargetUnits));
                OnPropertyChanged(nameof(TargetAssetDisplayName));
                OnPropertyChanged(nameof(ConfirmSummaryCostBasis));
                OnPropertyChanged(nameof(ShowNote));
                OnPropertyChanged(nameof(ShowCancelButton));
                OnPropertyChanged(nameof(CanChangeType));
            }
        }
    }

    public bool IsMergerFieldsStep => IsMerger && Step == CorporateActionFormStep.Fields;
    public bool IsMergerConfirmStep => IsMerger && Step == CorporateActionFormStep.Confirm;
    public bool ShowNote => !IsMergerConfirmStep;
    public bool ShowCancelButton => !IsMergerConfirmStep;
    public bool CanChangeType => IsEditable && !IsMergerConfirmStep;

    public string ConfirmSummary =>
        $"Your position in {SourceAssetName} ({SourceQuantity:N2} units) will close and convert into " +
        $"{ConfirmSummaryTargetUnits} of {TargetAssetDisplayName}, carrying over " +
        $"{ConfirmSummaryCostBasis} of cost basis.";

    public string ConfirmSummaryTargetUnits =>
        $"{FormatMoney(SourceQuantity * ExchangeRatio)} units";

    public string ConfirmSummaryCostBasis =>
        FormatMoney(SourceCostBasis);

    public string TargetAssetDisplayName =>
        TargetAssetPicker?.AssetName ?? TargetAssetName;

    public string SpinOffNewAssetDisplayName =>
        string.IsNullOrWhiteSpace(TargetAssetDisplayName) ? "the new asset" : TargetAssetDisplayName;

    public string SpinOffStaysWithParentAmount =>
        FormatMoney(SourceCostBasis * (1 - AllocationPercentage / 100));

    public string SpinOffMovesToNewAmount =>
        FormatMoney(SourceCostBasis * (AllocationPercentage / 100));

    private static string FormatMoney(decimal value) => value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);

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
                OnPropertyChanged(nameof(IsSplit));
                OnPropertyChanged(nameof(IsMerger));
                OnPropertyChanged(nameof(IsSpinOff));
                OnPropertyChanged(nameof(IsMergerOrSpinOff));
                OnPropertyChanged(nameof(IsMergerFieldsStep));
                OnPropertyChanged(nameof(IsMergerConfirmStep));
                OnPropertyChanged(nameof(ConfirmLabel));
                OnPropertyChanged(nameof(ShowNote));
                OnPropertyChanged(nameof(ShowCancelButton));
                OnPropertyChanged(nameof(CanChangeType));
                OnPropertyChanged(nameof(TargetAssetDisplayName));
                OnPropertyChanged(nameof(SpinOffNewAssetDisplayName));
                OnPropertyChanged(nameof(SpinOffStaysWithParentAmount));
                OnPropertyChanged(nameof(SpinOffMovesToNewAmount));
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

    public decimal ExchangeRatio
    {
        get => _exchangeRatio;
        set
        {
            if (SetProperty(ref _exchangeRatio, value))
            {
                OnPropertyChanged(nameof(ConfirmSummary));
                OnPropertyChanged(nameof(ConfirmSummaryTargetUnits));
                Validate();
            }
        }
    }

    public decimal? CashInLieuAmount
    {
        get => _cashInLieuAmount;
        set => SetProperty(ref _cashInLieuAmount, value);
    }

    public decimal QuantityReceived
    {
        get => _quantityReceived;
        set
        {
            if (SetProperty(ref _quantityReceived, value))
            {
                Validate();
            }
        }
    }

    public decimal AllocationPercentage
    {
        get => _allocationPercentage;
        set
        {
            if (SetProperty(ref _allocationPercentage, value))
            {
                OnPropertyChanged(nameof(SpinOffStaysWithParentAmount));
                OnPropertyChanged(nameof(SpinOffMovesToNewAmount));
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
    public RelayCommand BackCommand { get; }

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
        string? note,
        TargetAssetPickerViewModel? targetAssetPicker,
        string targetAssetName,
        string sourceAssetName,
        decimal sourceQuantity,
        decimal sourceCostBasis,
        decimal exchangeRatio,
        decimal? cashInLieuAmount,
        decimal quantityReceived,
        decimal allocationPercentage)
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
        _exchangeRatio = exchangeRatio;
        _cashInLieuAmount = cashInLieuAmount;
        _quantityReceived = quantityReceived;
        _allocationPercentage = allocationPercentage;

        TargetAssetPicker = targetAssetPicker;
        TargetAssetName = targetAssetName;
        SourceAssetName = sourceAssetName;
        SourceQuantity = sourceQuantity;
        SourceCostBasis = sourceCostBasis;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);
        BackCommand = new RelayCommand(Back);

        if (TargetAssetPicker != null)
        {
            TargetAssetPicker.PropertyChanged += OnTargetAssetPickerPropertyChanged;
        }

        Validate();
    }

    private void OnTargetAssetPickerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TargetAssetPickerViewModel.AssetName))
        {
            return;
        }

        OnPropertyChanged(nameof(ConfirmSummary));
        OnPropertyChanged(nameof(TargetAssetDisplayName));
        OnPropertyChanged(nameof(SpinOffNewAssetDisplayName));
    }

    public static CorporateActionFormViewModel CreateForAdd(
        string brokerName,
        string portfolioName,
        string assetName,
        TargetAssetPickerViewModel? targetAssetPicker = null,
        decimal sourceQuantity = 0m,
        decimal sourceCostBasis = 0m) =>
        new(CorporateActionFormMode.Add, brokerName, portfolioName, assetName, Guid.Empty, DateTime.Today,
            CorporateActionFormValidation.SplitTypeValue, 0m, 0m, null,
            targetAssetPicker ?? new TargetAssetPickerViewModel(Array.Empty<AssetAdminDTO>()),
            string.Empty, assetName, sourceQuantity, sourceCostBasis, 0m, null, 0m, 0m);

    public static CorporateActionFormViewModel CreateForUpdate(
        string brokerName,
        string portfolioName,
        string assetName,
        Guid id,
        DateTime effectiveDate,
        string type,
        decimal ratioNumerator,
        decimal ratioDenominator,
        string? note,
        string targetAssetName = "",
        string? sourceAssetName = null,
        decimal exchangeRatio = 0m,
        decimal? cashInLieuAmount = null,
        decimal sourceQuantity = 0m,
        decimal sourceCostBasis = 0m,
        decimal quantityReceived = 0m,
        decimal allocationPercentage = 0m) =>
        new(CorporateActionFormMode.Update, brokerName, portfolioName, assetName, id, effectiveDate, type, ratioNumerator, ratioDenominator, note,
            null, targetAssetName, sourceAssetName ?? assetName, sourceQuantity, sourceCostBasis, exchangeRatio, cashInLieuAmount,
            quantityReceived, allocationPercentage);

    public static CorporateActionFormViewModel CreateForDelete(
        string brokerName,
        string portfolioName,
        string assetName,
        Guid id,
        DateTime effectiveDate,
        string type,
        decimal ratioNumerator,
        decimal ratioDenominator,
        string? note,
        string targetAssetName = "",
        string? sourceAssetName = null,
        decimal exchangeRatio = 0m,
        decimal? cashInLieuAmount = null,
        decimal sourceQuantity = 0m,
        decimal sourceCostBasis = 0m,
        decimal quantityReceived = 0m,
        decimal allocationPercentage = 0m) =>
        new(CorporateActionFormMode.Delete, brokerName, portfolioName, assetName, id, effectiveDate, type, ratioNumerator, ratioDenominator, note,
            null, targetAssetName, sourceAssetName ?? assetName, sourceQuantity, sourceCostBasis, exchangeRatio, cashInLieuAmount,
            quantityReceived, allocationPercentage);

    private void Confirm()
    {
        if (IsMergerFieldsStep)
        {
            Validate();
            if (!CanConfirm())
            {
                return;
            }

            Step = CorporateActionFormStep.Confirm;
            return;
        }

        Validate();
        if (!CanConfirm())
        {
            return;
        }

        CloseRequested?.Invoke(this, true);
    }

    private void Back()
    {
        Step = CorporateActionFormStep.Fields;
        ValidationMessage = string.Empty;
        ConfirmCommand.RaiseCanExecuteChanged();
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    /// <summary>Surfaces a server-side refusal inline, in the same slot as client-side validation.
    /// Reusing <see cref="ValidationMessage"/> also disables Confirm until the user changes a field,
    /// since resubmitting the same values would fail identically - except on the Merger confirm step,
    /// where every field is hidden, so <see cref="CanConfirm"/> lets the same values be retried directly.</summary>
    public void ReportSubmitFailed(string message)
    {
        ValidationMessage = message;
        ConfirmCommand.RaiseCanExecuteChanged();
    }

    private bool CanConfirm()
    {
        if (Mode == CorporateActionFormMode.Delete || IsMergerConfirmStep)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(ValidationMessage);
    }

    private void Validate()
    {
        ValidationMessage = CorporateActionFormValidation.BuildValidationMessage(
            Mode == CorporateActionFormMode.Delete,
            Type,
            Mode == CorporateActionFormMode.Add,
            EffectiveDate,
            RatioNumerator,
            RatioDenominator,
            IsMerger || IsSpinOff ? TargetAssetDisplayName : string.Empty,
            ExchangeRatio,
            QuantityReceived,
            AllocationPercentage);
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
