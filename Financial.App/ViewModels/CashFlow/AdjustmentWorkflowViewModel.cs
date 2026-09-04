using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class AdjustmentWorkflowViewModel : ViewModelBase
{
    private readonly IBalanceAdjustmentService _balanceAdjustmentService;
    private readonly ObservableCollection<BankTotalRow> _bankTotals;
    private readonly Func<Task> _refresh;

    private bool _isAdjustmentFormOpen;
    private Guid? _editingAdjustmentBank;
    private Guid? _editingAdjustmentId;
    private Guid? _adjustmentFormBankName;
    private decimal _adjustmentFormCurrentBalance;
    private DateTime? _adjustmentFormDate;
    private string _adjustmentFormTargetBalance = string.Empty;
    private string _adjustmentFormNote = string.Empty;
    private bool _isSavingAdjustment;
    private string? _adjustmentSaveError;
    private decimal? _adjustmentSavedDelta;

    // Persistent create-form defaults (P38-F10) - read on the next ShowCreateAdjustmentForm,
    // written back after every successful save.
    private DateTime? _lastUsedAdjustmentDate;
    private Guid? _lastUsedAdjustmentBank;

    /// <summary>The same instance MonthlyViewModel owns — mutated in place by its refresh, never replaced.</summary>
    public ObservableCollection<BankDTO> Banks { get; }

    public bool IsAdjustmentFormOpen
    {
        get => _isAdjustmentFormOpen;
        private set => SetProperty(ref _isAdjustmentFormOpen, value);
    }

    public bool IsEditingAdjustment => _editingAdjustmentId != null;

    public Guid? AdjustmentFormBankName
    {
        get => _adjustmentFormBankName;
        set
        {
            if (SetProperty(ref _adjustmentFormBankName, value))
            {
                AdjustmentFormCurrentBalance = _bankTotals.FirstOrDefault(b => b.BankId == value)?.Balance ?? 0m;
                OnPropertyChanged(nameof(IsAdjustmentBankSelected));
                OnPropertyChanged(nameof(AdjustmentFormBankDisplayName));
                SaveAdjustmentCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public string AdjustmentFormBankDisplayName => Banks.FirstOrDefault(b => b.Id == AdjustmentFormBankName)?.Name ?? string.Empty;

    public bool IsAdjustmentBankSelected => AdjustmentFormBankName is not null;

    public decimal AdjustmentFormCurrentBalance
    {
        get => _adjustmentFormCurrentBalance;
        private set => SetProperty(ref _adjustmentFormCurrentBalance, value);
    }

    public DateTime? AdjustmentFormDate
    {
        get => _adjustmentFormDate;
        set => SetProperty(ref _adjustmentFormDate, value);
    }

    public string AdjustmentFormTargetBalance
    {
        get => _adjustmentFormTargetBalance;
        set => SetProperty(ref _adjustmentFormTargetBalance, value);
    }

    public string AdjustmentFormNote
    {
        get => _adjustmentFormNote;
        set => SetProperty(ref _adjustmentFormNote, value);
    }

    public bool IsSavingAdjustment
    {
        get => _isSavingAdjustment;
        private set => SetProperty(ref _isSavingAdjustment, value);
    }

    public string? AdjustmentSaveError
    {
        get => _adjustmentSaveError;
        private set
        {
            if (SetProperty(ref _adjustmentSaveError, value))
            {
                OnPropertyChanged(nameof(TargetBalanceFieldError));
                OnPropertyChanged(nameof(AdjustmentGeneralSaveError));
            }
        }
    }

    /// <summary>
    /// Per-field validation error (P38-F02) — Target Balance is the only field a save error can
    /// ever target (the Bank picker is validated client-side by disabling Save until a bank is
    /// chosen). Matches both the server's rejection text (Domain's <c>BalanceAdjustment</c>,
    /// "cannot be negative" — mirroring mapBalanceAdjustmentErrorToField.ts on the Web side) and
    /// this form's own client-side <see cref="BalanceAdjustmentFormValidation"/> text ("zero or
    /// greater").
    /// </summary>
    public string? TargetBalanceFieldError =>
        AdjustmentSaveError is { } error &&
        (error.Contains("cannot be negative", StringComparison.OrdinalIgnoreCase) ||
         error.Contains("zero or greater", StringComparison.OrdinalIgnoreCase))
            ? error
            : null;

    /// <summary>Bottom-of-form message — shown only when the error isn't already attributed to a field above.</summary>
    public string? AdjustmentGeneralSaveError => TargetBalanceFieldError is null ? AdjustmentSaveError : null;

    public decimal? AdjustmentSavedDelta
    {
        get => _adjustmentSavedDelta;
        private set
        {
            if (SetProperty(ref _adjustmentSavedDelta, value))
            {
                OnPropertyChanged(nameof(HasAdjustmentResult));
                OnPropertyChanged(nameof(ShowAdjustmentForm));
            }
        }
    }

    public bool HasAdjustmentResult => AdjustmentSavedDelta != null;

    public bool ShowAdjustmentForm => AdjustmentSavedDelta == null;

    public RelayCommand ShowCorrectBalanceFormCommand { get; }
    public RelayCommand<BalanceAdjustmentDTO> EditAdjustmentCommand { get; }
    public RelayCommand CancelAdjustmentFormCommand { get; }
    public RelayCommand SaveAdjustmentCommand { get; }
    public RelayCommand DismissAdjustmentResultCommand { get; }

    public AdjustmentWorkflowViewModel(
        IBalanceAdjustmentService balanceAdjustmentService,
        ObservableCollection<BankDTO> banks,
        ObservableCollection<BankTotalRow> bankTotals,
        Func<Task> refresh)
    {
        _balanceAdjustmentService = balanceAdjustmentService ?? throw new ArgumentNullException(nameof(balanceAdjustmentService));
        Banks = banks ?? throw new ArgumentNullException(nameof(banks));
        _bankTotals = bankTotals ?? throw new ArgumentNullException(nameof(bankTotals));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        ShowCorrectBalanceFormCommand = new RelayCommand(ShowCreateAdjustmentForm);
        EditAdjustmentCommand = new RelayCommand<BalanceAdjustmentDTO>(ShowEditAdjustmentForm);
        CancelAdjustmentFormCommand = new RelayCommand(CloseAdjustmentForm);
        SaveAdjustmentCommand = new RelayCommand(async () => await SaveAdjustmentAsync(), () => !IsSavingAdjustment && IsAdjustmentBankSelected);
        DismissAdjustmentResultCommand = new RelayCommand(() => AdjustmentSavedDelta = null);
    }

    private void ShowCreateAdjustmentForm()
    {
        _editingAdjustmentBank = null;
        _editingAdjustmentId = null;
        AdjustmentFormDate = _lastUsedAdjustmentDate ?? DateTime.Today;
        AdjustmentFormTargetBalance = string.Empty;
        AdjustmentFormNote = string.Empty;
        AdjustmentSaveError = null;
        AdjustmentSavedDelta = null;
        AdjustmentFormBankName = _lastUsedAdjustmentBank is { } lastBank && Banks.Any(b => b.Id == lastBank) ? lastBank : null;
        OnPropertyChanged(nameof(IsEditingAdjustment));
        IsAdjustmentFormOpen = true;
    }

    private void ShowEditAdjustmentForm(BalanceAdjustmentDTO? adjustment)
    {
        if (adjustment is null)
        {
            return;
        }

        _editingAdjustmentBank = adjustment.BankId;
        _editingAdjustmentId = adjustment.Id;
        AdjustmentFormDate = adjustment.Date.ToDateTime(TimeOnly.MinValue);
        AdjustmentFormTargetBalance = adjustment.TargetBalance.ToString("0.##");
        AdjustmentFormNote = adjustment.Note ?? string.Empty;
        AdjustmentSaveError = null;
        AdjustmentSavedDelta = null;
        AdjustmentFormBankName = adjustment.BankId;
        OnPropertyChanged(nameof(IsEditingAdjustment));
        IsAdjustmentFormOpen = true;
    }

    private void CloseAdjustmentForm()
    {
        IsAdjustmentFormOpen = false;
        _editingAdjustmentBank = null;
        _editingAdjustmentId = null;
        AdjustmentSaveError = null;
        AdjustmentSavedDelta = null;
    }

    internal Task SaveAdjustmentAsync() => ExecuteSaveAsync(
        () => BalanceAdjustmentFormValidation.BuildValidationMessage(AdjustmentFormDate, AdjustmentFormTargetBalance),
        error => AdjustmentSaveError = error,
        saving => IsSavingAdjustment = saving,
        async () =>
        {
            var date = DateOnly.FromDateTime(AdjustmentFormDate!.Value);
            var targetBalance = decimal.Parse(AdjustmentFormTargetBalance);
            var note = string.IsNullOrWhiteSpace(AdjustmentFormNote) ? null : AdjustmentFormNote;

            BalanceAdjustmentDTO result;
            if (_editingAdjustmentId is { } id && _editingAdjustmentBank is { } bank)
            {
                result = await _balanceAdjustmentService.UpdateAdjustmentAsync(bank, id, new BalanceAdjustmentUpdateDTO
                {
                    Date = date, TargetBalance = targetBalance, Note = note,
                });
            }
            else
            {
                result = await _balanceAdjustmentService.AddAdjustmentAsync(AdjustmentFormBankName!.Value, new BalanceAdjustmentCreateDTO
                {
                    Date = date, TargetBalance = targetBalance, Note = note,
                });
            }

            _lastUsedAdjustmentDate = AdjustmentFormDate;
            _lastUsedAdjustmentBank = AdjustmentFormBankName;

            await _refresh();
            AdjustmentSavedDelta = result.Delta;
        },
        SaveAdjustmentCommand.RaiseCanExecuteChanged);
}
