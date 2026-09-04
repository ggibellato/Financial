using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Shared.Abstractions.Observability;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class ExpenseWorkflowViewModel : ViewModelBase
{
    private const string SettledStatus = "CreditCardSettled";

    private readonly IExpenseService _expenseService;
    private readonly ObservableCollection<CategoryDTO> _categories;
    private readonly ObservableCollection<CreditCardDTO> _creditCards;
    private readonly Func<string, bool> _confirm;
    private readonly ITelemetryTracer _tracer;
    private readonly Func<Task> _refresh;

    private bool _isExpenseFormOpen;
    private Guid? _editingExpenseId;
    private DateTime? _expenseFormDate;
    private string _expenseFormDescription = string.Empty;
    private Guid? _expenseFormCategoryId;
    private string _expenseFormValue = string.Empty;
    private bool _isCardPaymentMode;
    private Guid? _expenseFormPaymentSource;
    private Guid? _expenseFormCreditCardId;
    private string _expenseFormCreditCardName = string.Empty;
    private string _expenseFormRoundUpAmount = string.Empty;
    // True until the user directly edits the round-up field, or an edit form loads a saved
    // amount - both "freeze" it so later Value/PaymentSource edits stop recomputing it.
    private bool _roundUpAmountIsAuto = true;
    private bool _expenseFormCountsAsTithe = true;
    private bool _expenseFormIsSettled;
    private int _expenseFormInvoiceYear;
    private int _expenseFormInvoiceMonth;
    private bool _invoiceDateTouchedByUser;
    private bool _isSavingExpense;
    private string? _expenseSaveError;
    private string? _deletingExpenseError;

    // Persisted for the workflow ViewModel's lifetime (the app's own lifetime - see
    // docs/ui/standard-compliance-audit-2026-08-29-forms.md's "persistent create-form defaults"
    // follow-up), not just this form's open/close cycle: read on the next ShowCreateExpenseForm,
    // written back after every successful save.
    private DateTime? _lastUsedExpenseDate;
    private Guid? _lastUsedExpensePaymentSource;
    private Guid? _lastUsedExpenseCreditCardId;
    private Guid? _lastUsedExpenseCategoryId;

    public ObservableCollection<ExpenseDTO> Expenses { get; } = [];
    public ObservableCollection<ExpenseDTO> UnpaidCardCharges { get; } = [];
    public ObservableCollection<ExpenseDTO> FilteredExpenses { get; } = [];
    public ObservableCollection<ExpenseDTO> FilteredUnpaidCardCharges { get; } = [];

    public ColumnFilterViewModel<ExpenseDTO> ExpensesCategoryFilter { get; }
    public ColumnFilterViewModel<ExpenseDTO> ExpensesBankFilter { get; }
    public ColumnFilterViewModel<ExpenseDTO> ExpensesCardFilter { get; }
    public ColumnFilterViewModel<ExpenseDTO> UnpaidCardChargesCategoryFilter { get; }
    public ColumnFilterViewModel<ExpenseDTO> UnpaidCardChargesCardFilter { get; }

    /// <summary>The same instance MonthlyViewModel owns — mutated in place by its refresh, never replaced.</summary>
    public ObservableCollection<BankDTO> Banks { get; }

    public IEnumerable<CategoryDTO> ActiveCategories => _categories.Where(c => c.Active);
    public IEnumerable<CreditCardDTO> ActiveCreditCards => _creditCards.Where(c => c.IsActive);

    public bool IsExpenseFormOpen
    {
        get => _isExpenseFormOpen;
        private set => SetProperty(ref _isExpenseFormOpen, value);
    }

    public bool IsEditingExpense => _editingExpenseId != null;

    public DateTime? ExpenseFormDate
    {
        get => _expenseFormDate;
        set
        {
            if (SetProperty(ref _expenseFormDate, value) && value.HasValue && IsCardPaymentMode && !_invoiceDateTouchedByUser)
            {
                SetDefaultInvoiceDate(value.Value.Year, value.Value.Month);
            }
        }
    }

    public string ExpenseFormDescription
    {
        get => _expenseFormDescription;
        set => SetProperty(ref _expenseFormDescription, value);
    }

    public Guid? ExpenseFormCategoryId
    {
        get => _expenseFormCategoryId;
        set
        {
            if (SetProperty(ref _expenseFormCategoryId, value))
            {
                OnPropertyChanged(nameof(ShowCountsAsTitheField));
            }
        }
    }

    public bool ShowCountsAsTitheField =>
        _categories.FirstOrDefault(c => c.Id == ExpenseFormCategoryId)?.IsTithe == true;

    public bool ExpenseFormCountsAsTithe
    {
        get => _expenseFormCountsAsTithe;
        set => SetProperty(ref _expenseFormCountsAsTithe, value);
    }

    public string ExpenseFormValue
    {
        get => _expenseFormValue;
        set
        {
            if (SetProperty(ref _expenseFormValue, value))
            {
                ApplyRoundUpSuggestionIfAuto();
            }
        }
    }

    public bool IsCardPaymentMode
    {
        get => _isCardPaymentMode;
        private set
        {
            if (SetProperty(ref _isCardPaymentMode, value))
            {
                OnPropertyChanged(nameof(IsBankPaymentMode));
                OnPropertyChanged(nameof(ShowRoundUpField));
            }
        }
    }

    public bool IsBankPaymentMode => !IsCardPaymentMode;

    public Guid? ExpenseFormPaymentSource
    {
        get => _expenseFormPaymentSource;
        set
        {
            if (SetProperty(ref _expenseFormPaymentSource, value))
            {
                OnPropertyChanged(nameof(ShowRoundUpField));
                ApplyRoundUpSuggestionIfAuto();
            }
        }
    }

    public Guid? ExpenseFormCreditCardId
    {
        get => _expenseFormCreditCardId;
        set
        {
            if (SetProperty(ref _expenseFormCreditCardId, value))
            {
                ApplyCardInvoiceDefaultIfAuto();
            }
        }
    }

    public string ExpenseFormCreditCardName
    {
        get => _expenseFormCreditCardName;
        private set => SetProperty(ref _expenseFormCreditCardName, value);
    }

    public string ExpenseFormRoundUpAmount
    {
        get => _expenseFormRoundUpAmount;
        set
        {
            _roundUpAmountIsAuto = false;
            SetProperty(ref _expenseFormRoundUpAmount, value);
        }
    }

    /// <summary>Recomputes the suggestion from the current Value/PaymentSource, unless the user
    /// has already typed into the round-up field directly - see <see cref="_roundUpAmountIsAuto"/>.</summary>
    private void ApplyRoundUpSuggestionIfAuto()
    {
        if (_roundUpAmountIsAuto)
        {
            ApplyRoundUpSuggestion(ShowRoundUpField ? SuggestRoundUpAmount() : string.Empty);
        }
    }

    /// <summary>Sets the round-up field to a computed value without marking it user-edited,
    /// so it stays eligible for further automatic recomputation.</summary>
    private void ApplyRoundUpSuggestion(string value)
    {
        SetProperty(ref _expenseFormRoundUpAmount, value, nameof(ExpenseFormRoundUpAmount));
        _roundUpAmountIsAuto = true;
    }

    public bool ShowRoundUpField =>
        !IsCardPaymentMode
        && Banks.FirstOrDefault(b => b.Id == ExpenseFormPaymentSource) is { RoundUpEnabled: true };

    public bool ExpenseFormIsSettled
    {
        get => _expenseFormIsSettled;
        private set
        {
            if (SetProperty(ref _expenseFormIsSettled, value))
            {
                OnPropertyChanged(nameof(ShowPaymentModeFields));
            }
        }
    }

    public bool ShowPaymentModeFields => !ExpenseFormIsSettled;

    public int ExpenseFormInvoiceYear
    {
        get => _expenseFormInvoiceYear;
        set
        {
            if (SetProperty(ref _expenseFormInvoiceYear, value))
            {
                _invoiceDateTouchedByUser = true;
            }
        }
    }

    public int ExpenseFormInvoiceMonth
    {
        get => _expenseFormInvoiceMonth;
        set
        {
            if (SetProperty(ref _expenseFormInvoiceMonth, value))
            {
                _invoiceDateTouchedByUser = true;
            }
        }
    }

    public bool IsSavingExpense
    {
        get => _isSavingExpense;
        private set => SetProperty(ref _isSavingExpense, value);
    }

    public string? ExpenseSaveError
    {
        get => _expenseSaveError;
        private set
        {
            if (SetProperty(ref _expenseSaveError, value))
            {
                OnPropertyChanged(nameof(DateFieldError));
                OnPropertyChanged(nameof(DescriptionFieldError));
                OnPropertyChanged(nameof(CategoryFieldError));
                OnPropertyChanged(nameof(ValueFieldError));
                OnPropertyChanged(nameof(PaymentModeFieldError));
                OnPropertyChanged(nameof(RoundUpAmountFieldError));
                OnPropertyChanged(nameof(ExpenseGeneralSaveError));
            }
        }
    }

    /// <summary>
    /// Per-field validation errors (P38-F04) — same substring-match pattern as F02's
    /// <c>AdjustmentWorkflowViewModel.TargetBalanceFieldError</c>, matching this form's own
    /// client-side <see cref="ExpenseFormValidation"/> text.
    /// </summary>
    public string? DateFieldError => MatchFieldError("Date is required.");

    public string? DescriptionFieldError => MatchFieldError("Description is required.");

    public string? CategoryFieldError => MatchFieldError("Category is required.");

    public string? ValueFieldError => MatchFieldError("Value must be a non-zero number.");

    public string? PaymentModeFieldError =>
        MatchFieldError("Card is required when charging to a card.", "Payment Source is required.");

    public string? RoundUpAmountFieldError => MatchFieldError("Round-up amount must be between");

    /// <summary>
    /// Returns just the one line of <see cref="ExpenseSaveError"/> matching this field, not the
    /// whole (possibly multi-line) combined message — so when several fields are invalid at once,
    /// each shows only its own text instead of repeating every error under every field.
    /// </summary>
    private string? MatchFieldError(params string[] fragments) =>
        ExpenseSaveError?.Split(Environment.NewLine)
            .FirstOrDefault(line => fragments.Any(f => line.Contains(f, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Bottom-of-form message — shown only when the error isn't already attributed to a field above.</summary>
    public string? ExpenseGeneralSaveError =>
        DateFieldError is null && DescriptionFieldError is null && CategoryFieldError is null &&
        ValueFieldError is null && PaymentModeFieldError is null && RoundUpAmountFieldError is null
            ? ExpenseSaveError
            : null;

    public string? DeletingExpenseError
    {
        get => _deletingExpenseError;
        private set => SetProperty(ref _deletingExpenseError, value);
    }

    public RelayCommand<string> ShowCreateExpenseFormCommand { get; }
    public RelayCommand CancelExpenseFormCommand { get; }
    public RelayCommand SaveExpenseCommand { get; }
    public RelayCommand<ExpenseDTO> EditExpenseCommand { get; }
    public RelayCommand<ExpenseDTO> DeleteExpenseCommand { get; }

    public ExpenseWorkflowViewModel(
        IExpenseService expenseService,
        ObservableCollection<CategoryDTO> categories,
        ObservableCollection<BankDTO> banks,
        ObservableCollection<CreditCardDTO> creditCards,
        Func<string, bool> confirm,
        ITelemetryTracer tracer,
        Func<Task> refresh)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        Banks = banks ?? throw new ArgumentNullException(nameof(banks));
        _creditCards = creditCards ?? throw new ArgumentNullException(nameof(creditCards));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        ShowCreateExpenseFormCommand = new RelayCommand<string>(ShowCreateExpenseForm);
        CancelExpenseFormCommand = new RelayCommand(CloseExpenseForm);
        SaveExpenseCommand = new RelayCommand(async () => await SaveExpenseAsync(), () => !IsSavingExpense);
        EditExpenseCommand = new RelayCommand<ExpenseDTO>(ShowEditExpenseForm);
        DeleteExpenseCommand = new RelayCommand<ExpenseDTO>(
            async expense => await DeleteExpenseAsync(expense),
            expense => expense?.PaymentStatus != SettledStatus);

        ExpensesCategoryFilter = new ColumnFilterViewModel<ExpenseDTO>("Category", e => [e.CategoryName], ApplyExpensesFilter);
        ExpensesBankFilter = new ColumnFilterViewModel<ExpenseDTO>("Bank", e => [e.PaymentSourceBankName], ApplyExpensesFilter);
        ExpensesCardFilter = new ColumnFilterViewModel<ExpenseDTO>("Card", e => [e.CreditCardName], ApplyExpensesFilter);
        UnpaidCardChargesCategoryFilter = new ColumnFilterViewModel<ExpenseDTO>("Category", e => [e.CategoryName], ApplyUnpaidCardChargesFilter);
        UnpaidCardChargesCardFilter = new ColumnFilterViewModel<ExpenseDTO>("Card", e => [e.CreditCardName], ApplyUnpaidCardChargesFilter);
    }

    /// <summary>Applies data the coordinator's own refresh already fetched — this workflow never fetches on its own.</summary>
    public void ApplyRefresh(IReadOnlyList<ExpenseDTO> expenses, IReadOnlyList<ExpenseDTO> unpaidCardCharges)
    {
        ReplaceAll(Expenses, expenses);
        ExpensesCategoryFilter.Refresh(Expenses);
        ExpensesBankFilter.Refresh(Expenses);
        ExpensesCardFilter.Refresh(Expenses);
        ApplyExpensesFilter();

        ReplaceAll(UnpaidCardCharges, unpaidCardCharges);
        UnpaidCardChargesCategoryFilter.Refresh(UnpaidCardCharges);
        UnpaidCardChargesCardFilter.Refresh(UnpaidCardCharges);
        ApplyUnpaidCardChargesFilter();
    }

    private void ApplyExpensesFilter() =>
        ReplaceAll(
            FilteredExpenses,
            Expenses.Where(e => ExpensesCategoryFilter.Matches(e) && ExpensesBankFilter.Matches(e) && ExpensesCardFilter.Matches(e)));

    private void ApplyUnpaidCardChargesFilter() =>
        ReplaceAll(FilteredUnpaidCardCharges, UnpaidCardCharges.Where(e => UnpaidCardChargesCategoryFilter.Matches(e) && UnpaidCardChargesCardFilter.Matches(e)));

    /// <summary>Categories/CreditCards are the coordinator's own shared collections, mutated in place - it calls
    /// this after replacing them so ActiveCategories/ActiveCreditCards re-query, mirroring how it re-notifies its own.</summary>
    internal void NotifyCategoriesChanged() => OnPropertyChanged(nameof(ActiveCategories));

    internal void NotifyCreditCardsChanged() => OnPropertyChanged(nameof(ActiveCreditCards));

    private void ShowCreateExpenseForm(string? mode)
    {
        _editingExpenseId = null;
        ExpenseFormDate = _lastUsedExpenseDate ?? DateTime.Today;
        ExpenseFormDescription = string.Empty;
        ExpenseFormCategoryId = _lastUsedExpenseCategoryId is { } lastCategoryId && _categories.Any(c => c.Id == lastCategoryId)
            ? lastCategoryId
            : ActiveCategories.FirstOrDefault()?.Id;
        ExpenseFormValue = string.Empty;
        IsCardPaymentMode = mode == "card";
        ExpenseFormPaymentSource = IsCardPaymentMode
            ? null
            : (_lastUsedExpensePaymentSource is { } lastPaymentSource && Banks.Any(b => b.Id == lastPaymentSource)
                ? lastPaymentSource
                : (Banks.Count > 0 ? Banks[0].Id : null));
        _invoiceDateTouchedByUser = false;
        ExpenseFormCreditCardId = IsCardPaymentMode
            ? (_lastUsedExpenseCreditCardId is { } lastCreditCardId && _creditCards.Any(c => c.Id == lastCreditCardId) ? lastCreditCardId : null)
            : null;
        // ExpenseFormCreditCardId's setter only fires the hook when the id actually changes,
        // which misses the (very common) case of it staying null - call it directly too.
        ApplyCardInvoiceDefaultIfAuto();
        ExpenseFormCreditCardName = string.Empty;
        ApplyRoundUpSuggestion(string.Empty);
        ExpenseFormCountsAsTithe = true;
        ExpenseFormIsSettled = false;
        ExpenseSaveError = null;
        OnPropertyChanged(nameof(IsEditingExpense));
        IsExpenseFormOpen = true;
    }

    private void ShowEditExpenseForm(ExpenseDTO? expense)
    {
        if (expense is null)
        {
            return;
        }

        _editingExpenseId = expense.Id;
        ExpenseFormDate = expense.Date.ToDateTime(TimeOnly.MinValue);
        ExpenseFormDescription = expense.Description;
        ExpenseFormCategoryId = expense.CategoryId;
        ExpenseFormValue = expense.Value.ToString("0.##");
        IsCardPaymentMode = expense.CreditCardId != null;
        ExpenseFormPaymentSource = expense.PaymentSourceBankId;
        ExpenseFormCreditCardId = expense.CreditCardId;
        ExpenseFormCreditCardName = expense.CreditCardName ?? string.Empty;
        if (expense.RoundUpAmount is { } savedRoundUpAmount)
        {
            // A saved amount is frozen (not auto-recomputed) - same as a user-typed one -
            // so re-editing Value/PaymentSource here doesn't silently change what was saved.
            ExpenseFormRoundUpAmount = savedRoundUpAmount.ToString("0.##");
        }
        else
        {
            ApplyRoundUpSuggestion(string.Empty);
        }
        ExpenseFormCountsAsTithe = expense.CountsAsTithe;
        ExpenseFormIsSettled = expense.PaymentStatus == SettledStatus;
        _invoiceDateTouchedByUser = false;
        if (IsCardPaymentMode)
        {
            var invoiceDate = expense.InvoiceDate ?? expense.ChargeDate ?? expense.Date;
            SetDefaultInvoiceDate(invoiceDate.Year, invoiceDate.Month);
        }
        ExpenseSaveError = null;
        OnPropertyChanged(nameof(IsEditingExpense));
        IsExpenseFormOpen = true;
    }

    private void CloseExpenseForm()
    {
        IsExpenseFormOpen = false;
        _editingExpenseId = null;
        ExpenseSaveError = null;
    }

    /// <summary>
    /// Recomputes the invoice default from the selected card's own history, unless the user has
    /// already typed into the invoice picker directly - see <see cref="_invoiceDateTouchedByUser"/>.
    /// Prefers the card's latest existing invoice month when it's ahead of the plain
    /// date-derived default (e.g. the card already has a later invoice queued up), otherwise
    /// falls back to the date-derived default exactly as before.
    /// </summary>
    private void ApplyCardInvoiceDefaultIfAuto()
    {
        if (!IsCardPaymentMode || _invoiceDateTouchedByUser || ExpenseFormDate is not { } date)
        {
            return;
        }

        var standardDefault = new DateOnly(date.Year, date.Month, 1);
        var latest = _creditCards.FirstOrDefault(c => c.Id == _expenseFormCreditCardId)?.LatestInvoiceDate;
        var target = latest is { } l && l > standardDefault ? l : standardDefault;
        SetDefaultInvoiceDate(target.Year, target.Month);
    }

    /// <summary>
    /// Sets the invoice year/month without marking it as user-touched, so ExpenseFormDate's
    /// setter keeps resyncing the default until the user actually edits the invoice picker.
    /// </summary>
    private void SetDefaultInvoiceDate(int year, int month)
    {
        SetProperty(ref _expenseFormInvoiceYear, year, nameof(ExpenseFormInvoiceYear));
        SetProperty(ref _expenseFormInvoiceMonth, month, nameof(ExpenseFormInvoiceMonth));
    }

    private string SuggestRoundUpAmount()
    {
        if (!decimal.TryParse(ExpenseFormValue, out var value) || value <= 0)
        {
            return string.Empty;
        }

        var suggestion = Math.Round((Math.Ceiling(value) - value) * 100, MidpointRounding.AwayFromZero) / 100;
        return suggestion.ToString("0.##");
    }

    internal async Task SaveExpenseAsync()
    {
        using var span = _tracer.StartSpan("App.MonthlyViewModel.SaveExpense");

        // ExecuteSaveAsync never throws - it reports validation rejections and save failures
        // through its return value (and the bound error property for the UI). Only the outcome
        // reaches the span, never the message, which may echo user-entered text (FR-014).
        var saved = await SaveExpenseCoreAsync();
        span.SetAttribute(
            TelemetryAttributeKeys.OperationResult,
            saved ? TelemetryOperationResults.Success : TelemetryOperationResults.Failed);
    }

    private Task<bool> SaveExpenseCoreAsync() => ExecuteSaveAsync(
        () => ExpenseFormValidation.BuildValidationMessage(
            ExpenseFormDate, ExpenseFormDescription, ExpenseFormCategoryId, ExpenseFormValue,
            IsCardPaymentMode, ExpenseFormPaymentSource, ExpenseFormCreditCardId, ShowRoundUpField, ExpenseFormRoundUpAmount),
        error => ExpenseSaveError = error,
        saving => IsSavingExpense = saving,
        async () =>
        {
            var date = DateOnly.FromDateTime(ExpenseFormDate!.Value);
            var value = decimal.Parse(ExpenseFormValue);
            var paymentSource = IsCardPaymentMode ? null : ExpenseFormPaymentSource;
            var creditCardId = IsCardPaymentMode ? ExpenseFormCreditCardId : null;
            var categoryId = ExpenseFormCategoryId!.Value;
            DateOnly? invoiceDate = IsCardPaymentMode ? new DateOnly(ExpenseFormInvoiceYear, ExpenseFormInvoiceMonth, 1) : null;
            decimal? roundUpAmount = ShowRoundUpField && decimal.TryParse(ExpenseFormRoundUpAmount, out var parsedRoundUp)
                ? parsedRoundUp
                : null;

            if (_editingExpenseId is { } id)
            {
                await _expenseService.UpdateExpenseAsync(id, new ExpenseUpdateDTO
                {
                    Date = date,
                    Description = ExpenseFormDescription,
                    Value = value,
                    CategoryId = categoryId,
                    PaymentSourceBankId = paymentSource,
                    CreditCardId = creditCardId,
                    InvoiceDate = invoiceDate,
                    RoundUpAmount = roundUpAmount,
                    CountsAsTithe = ExpenseFormCountsAsTithe,
                });
            }
            else
            {
                await _expenseService.AddExpenseAsync(new ExpenseCreateDTO
                {
                    Date = date,
                    Description = ExpenseFormDescription,
                    Value = value,
                    CategoryId = categoryId,
                    PaymentSourceBankId = paymentSource,
                    CreditCardId = creditCardId,
                    InvoiceDate = invoiceDate,
                    RoundUpAmount = roundUpAmount,
                    CountsAsTithe = ExpenseFormCountsAsTithe,
                });
            }

            _lastUsedExpenseDate = ExpenseFormDate;
            _lastUsedExpenseCategoryId = ExpenseFormCategoryId;
            if (IsCardPaymentMode)
            {
                _lastUsedExpenseCreditCardId = ExpenseFormCreditCardId;
            }
            else
            {
                _lastUsedExpensePaymentSource = ExpenseFormPaymentSource;
            }

            CloseExpenseForm();
            await _refresh();
        },
        SaveExpenseCommand.RaiseCanExecuteChanged);

    internal async Task DeleteExpenseAsync(ExpenseDTO? expense)
    {
        if (expense is null)
        {
            return;
        }

        if (!_confirm($"Delete \"{expense.Description}\"? This removes it for good."))
        {
            return;
        }

        DeletingExpenseError = null;

        try
        {
            await _expenseService.DeleteExpenseAsync(expense.Id);
            await _refresh();
        }
        catch (Exception ex)
        {
            DeletingExpenseError = ex.Message;
        }
    }
}
