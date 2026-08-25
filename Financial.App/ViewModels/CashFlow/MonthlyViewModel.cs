using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Shared.Abstractions.Observability;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class MonthlyViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly IIncomeService _incomeService;
    private readonly IBankService _bankService;
    private readonly IIncomeSourceService _incomeSourceService;
    private readonly ITitheService _titheService;
    private readonly ITransferService _transferService;
    private readonly IBalanceAdjustmentService _balanceAdjustmentService;
    private readonly ICardStatementService _cardStatementService;
    private readonly ICreditCardService _creditCardService;
    private readonly ICategoryService _categoryService;
    private readonly ITelemetryTracer _tracer;

    private int _year;
    private int _month;
    private bool _isLoading = true;
    private string? _error;

    public int Year
    {
        get => _year;
        set
        {
            if (SetProperty(ref _year, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    public int Month
    {
        get => _month;
        set
        {
            if (SetProperty(ref _month, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public bool HasError => Error != null;

    public bool ShowContent => !IsLoading && !HasError;

    public ObservableCollection<ExpenseDTO> Expenses { get; } = [];
    public ObservableCollection<ExpenseDTO> UnpaidCardCharges { get; } = [];
    public ObservableCollection<CategoryTotalDTO> CategoryTotals { get; } = [];
    public ObservableCollection<BankDTO> Banks { get; } = [];
    public ObservableCollection<BankTotalRow> BankTotals { get; } = [];
    public ObservableCollection<CardStatementDTO> CardStatements { get; } = [];
    public ObservableCollection<IncomeTotalRow> IncomeTotals { get; } = [];
    public ObservableCollection<CreditCardDTO> CreditCards { get; } = [];
    public ObservableCollection<CategoryDTO> Categories { get; } = [];

    public IEnumerable<CreditCardDTO> ActiveCreditCards => CreditCards.Where(c => c.IsActive);

    public IEnumerable<CreditCardManagementRow> CreditCardManagementRows =>
        CreditCards.Select(c => new CreditCardManagementRow
        {
            CreditCard = c,
            Statement = CardStatements.FirstOrDefault(s => s.CreditCardId == c.Id),
        });

    public IEnumerable<CategoryDTO> ActiveCategories => Categories.Where(c => c.Active);

    public decimal BankTotalsSum => BankTotals.Sum(b => b.Balance);
    public decimal RoundUpTotalsSum => BankTotals.Sum(b => b.RoundUpTotal);
    public decimal AdjustmentTotal => CardStatements.Sum(s => s.OutstandingTotal);
    public decimal TotalIncoming => IncomeTotals.Sum(i => i.NetValue);

    private TitheSummaryDTO? _titheSummary;
    public TitheSummaryDTO? TitheSummary
    {
        get => _titheSummary;
        private set => SetProperty(ref _titheSummary, value);
    }

    public decimal CategoryTotalsSum => CategoryTotals.Sum(c => c.TotalValue);

    public RelayCommand RetryCommand { get; }

    public IncomeWorkflowViewModel Income { get; }

    private readonly Func<string, bool> _confirm;

    public MonthlyViewModel(
        IExpenseService expenseService,
        IIncomeService incomeService,
        IBankService bankService,
        IIncomeSourceService incomeSourceService,
        ITitheService titheService,
        ITransferService transferService,
        IBalanceAdjustmentService balanceAdjustmentService,
        ICardStatementService cardStatementService,
        ICreditCardService creditCardService,
        ICategoryService categoryService,
        Func<string, bool> confirm,
        ITelemetryTracer tracer)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _incomeSourceService = incomeSourceService ?? throw new ArgumentNullException(nameof(incomeSourceService));
        _titheService = titheService ?? throw new ArgumentNullException(nameof(titheService));
        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
        _balanceAdjustmentService = balanceAdjustmentService ?? throw new ArgumentNullException(nameof(balanceAdjustmentService));
        _cardStatementService = cardStatementService ?? throw new ArgumentNullException(nameof(cardStatementService));
        _creditCardService = creditCardService ?? throw new ArgumentNullException(nameof(creditCardService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));

        var today = DateTime.Today;
        _year = today.Year;
        _month = today.Month;

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        Income = new IncomeWorkflowViewModel(incomeService, confirm, RefreshAsync);
        InitializeExpenseCommands();
        InitializeBankCommands();
        InitializeTransferCommands();
        InitializeAdjustmentCommands();
        InitializeCardCommands();
    }

    private int _refreshRequestId;

    /// <summary>
    /// Reloads expenses/incomes/category totals/banks/tithe for the selected period. The caller
    /// is responsible for triggering the initial load (e.g. the hosting view's Loaded event).
    /// Guards against overlapping calls (e.g. a rapid year/month change racing a manual retry)
    /// by discarding a completion whose request has been superseded.
    /// </summary>
    internal async Task RefreshAsync()
    {
        var requestId = ++_refreshRequestId;
        IsLoading = true;
        Error = null;

        try
        {
            var year = Year;
            var month = Month;

            var expensesTask = Task.Run(() => _expenseService.GetExpensesByMonth(year, month));
            var unpaidCardChargesTask = Task.Run(() => _expenseService.GetUnpaidCardChargesByMonth(year, month));
            var incomesTask = Task.Run(() => _incomeService.GetIncomesByMonth(year, month));
            var categoryTotalsTask = Task.Run(() => _expenseService.GetCategoryTotalsByMonth(year, month));
            var banksTask = Task.Run(() => _bankService.GetBanks());
            var incomeSourcesTask = Task.Run(() => _incomeSourceService.GetIncomeSources());
            var bankBalancesTask = Task.Run(() => _bankService.GetBankBalancesByMonth(year, month));
            var titheSummaryTask = Task.Run(() => _titheService.GetTitheSummary(year, month));
            var transfersTask = Task.Run(() => _transferService.GetTransfersByMonth(year, month));
            var cardStatementsTask = _cardStatementService.GetStatementsForMonthAsync(year, month);
            var creditCardsTask = Task.Run(() => _creditCardService.GetCreditCards());
            var categoriesTask = Task.Run(() => _categoryService.GetCategories());

            await Task.WhenAll(
                expensesTask, unpaidCardChargesTask, incomesTask, categoryTotalsTask, banksTask,
                incomeSourcesTask, bankBalancesTask, titheSummaryTask, transfersTask, cardStatementsTask,
                creditCardsTask, categoriesTask);

            var expenses = expensesTask.Result;
            var unpaidCardCharges = unpaidCardChargesTask.Result;
            var incomes = incomesTask.Result;
            var categoryTotals = categoryTotalsTask.Result;
            var banks = banksTask.Result;
            var incomeSources = incomeSourcesTask.Result;
            var bankBalances = bankBalancesTask.Result;
            var titheSummary = titheSummaryTask.Result;
            var transfers = transfersTask.Result;
            var cardStatements = cardStatementsTask.Result;
            var creditCards = creditCardsTask.Result;
            var categories = categoriesTask.Result;

            var adjustmentsByBank = await Task.WhenAll(banks.Select(bank =>
                Task.Run(() => _balanceAdjustmentService.GetAdjustmentsByBank(bank.Id))));

            if (requestId != _refreshRequestId)
            {
                return;
            }

            ReplaceAll(Expenses, expenses);
            ReplaceAll(UnpaidCardCharges, unpaidCardCharges);
            ReplaceAll(CategoryTotals, categoryTotals);
            ReplaceAll(Banks, banks);
            Income.ApplyRefresh(incomes, incomeSources, banks);
            TitheSummary = titheSummary;
            OnPropertyChanged(nameof(CategoryTotalsSum));

            var newBankTotals = BuildBankTotals(banks, expenses, bankBalances);
            ReplaceAll(BankTotals, newBankTotals);
            OnPropertyChanged(nameof(BankTotalsSum));
            OnPropertyChanged(nameof(RoundUpTotalsSum));

            ReplaceAll(BankOperations, BuildBankOperations(transfers, adjustmentsByBank, year, month));
            BankFilterOptions = BuildBankFilterOptions(banks);
            ApplyBankFilter();

            ReplaceAll(CardStatements, cardStatements);
            OnPropertyChanged(nameof(AdjustmentTotal));

            ReplaceAll(CreditCards, creditCards);
            OnPropertyChanged(nameof(ActiveCreditCards));
            OnPropertyChanged(nameof(CreditCardManagementRows));

            ReplaceAll(Categories, categories);
            OnPropertyChanged(nameof(ActiveCategories));

            ReplaceAll(IncomeTotals, BuildIncomeTotals(incomes));
            OnPropertyChanged(nameof(TotalIncoming));
        }
        catch (Exception ex)
        {
            if (requestId == _refreshRequestId)
            {
                Error = ex.Message;
            }
        }
        finally
        {
            if (requestId == _refreshRequestId)
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>Mirrors useMonthly.ts's bankTotals: balance from the month's running total, round-up summed client-side from that bank's expenses.</summary>
    private static List<BankTotalRow> BuildBankTotals(
        IReadOnlyList<BankDTO> banks,
        IReadOnlyList<ExpenseDTO> expenses,
        IReadOnlyList<BankBalanceDTO> bankBalances)
    {
        return banks.Select(bank =>
        {
            var roundUpTotal = expenses
                .Where(e => e.PaymentSourceBankId == bank.Id)
                .Sum(e => e.RoundUpAmount ?? 0m);
            var balance = bankBalances.FirstOrDefault(b => b.Bank == bank.Name)?.Balance ?? 0m;
            return new BankTotalRow { BankId = bank.Id, Bank = bank.Name, Balance = balance, RoundUpTotal = roundUpTotal };
        }).ToList();
    }

    /// <summary>Mirrors useMonthly.ts's incomeTotals: net summed per source always, gross summed only across entries that report one.</summary>
    private static List<IncomeTotalRow> BuildIncomeTotals(IReadOnlyList<IncomeDTO> incomes)
    {
        return incomes
            .GroupBy(i => i.IncomeSourceName)
            .Select(group => new IncomeTotalRow
            {
                Source = group.Key,
                NetValue = group.Sum(i => i.NetValue),
                GrossValue = group.Any(i => i.GrossValue.HasValue)
                    ? group.Sum(i => i.GrossValue ?? 0m)
                    : null,
            })
            .ToList();
    }

    private static List<BankOperationRow> BuildBankOperations(
        IReadOnlyList<TransferDTO> transfers,
        IReadOnlyList<IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank,
        int year,
        int month)
    {
        // Transfers are already month-scoped by ITransferService.GetTransfersByMonth; only
        // adjustments (fetched per bank via GetAdjustmentsByBank, not month-scoped) need filtering here.
        var rows = new List<BankOperationRow>(transfers.Select(BankOperationRow.FromTransfer));

        rows.AddRange(adjustmentsByBank
            .SelectMany(adjustments => adjustments)
            .Where(a => a.Date.Year == year && a.Date.Month == month)
            .Select(BankOperationRow.FromAdjustment));

        return rows.OrderByDescending(r => r.Date).ToList();
    }

    /// <summary>Options for the Bank tab's filter dropdown: "All Banks" plus each configured bank name.</summary>
    private static IReadOnlyList<string> BuildBankFilterOptions(IReadOnlyList<BankDTO> banks) =>
        new[] { AllBanksFilter }.Concat(banks.Select(b => b.Name)).ToList();

    private const string SettledStatus = "CreditCardSettled";

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
    private bool _expenseFormCountsAsTithe = true;
    private bool _expenseFormIsSettled;
    private int _expenseFormInvoiceYear;
    private int _expenseFormInvoiceMonth;
    private bool _invoiceDateTouchedByUser;
    private bool _isSavingExpense;
    private string? _expenseSaveError;
    private string? _deletingExpenseError;

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
        Categories.FirstOrDefault(c => c.Id == ExpenseFormCategoryId)?.IsTithe == true;

    public bool ExpenseFormCountsAsTithe
    {
        get => _expenseFormCountsAsTithe;
        set => SetProperty(ref _expenseFormCountsAsTithe, value);
    }

    public string ExpenseFormValue
    {
        get => _expenseFormValue;
        set => SetProperty(ref _expenseFormValue, value);
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
                if (ShowRoundUpField)
                {
                    ExpenseFormRoundUpAmount = SuggestRoundUpAmount();
                }
            }
        }
    }

    public Guid? ExpenseFormCreditCardId
    {
        get => _expenseFormCreditCardId;
        set => SetProperty(ref _expenseFormCreditCardId, value);
    }

    public string ExpenseFormCreditCardName
    {
        get => _expenseFormCreditCardName;
        private set => SetProperty(ref _expenseFormCreditCardName, value);
    }

    public string ExpenseFormRoundUpAmount
    {
        get => _expenseFormRoundUpAmount;
        set => SetProperty(ref _expenseFormRoundUpAmount, value);
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

    /// <summary>
    /// Sets the invoice year/month without marking it as user-touched, so ExpenseFormDate's
    /// setter keeps resyncing the default until the user actually edits the invoice picker.
    /// </summary>
    private void SetDefaultInvoiceDate(int year, int month)
    {
        SetProperty(ref _expenseFormInvoiceYear, year, nameof(ExpenseFormInvoiceYear));
        SetProperty(ref _expenseFormInvoiceMonth, month, nameof(ExpenseFormInvoiceMonth));
    }

    public bool IsSavingExpense
    {
        get => _isSavingExpense;
        private set => SetProperty(ref _isSavingExpense, value);
    }

    public string? ExpenseSaveError
    {
        get => _expenseSaveError;
        private set => SetProperty(ref _expenseSaveError, value);
    }

    public string? DeletingExpenseError
    {
        get => _deletingExpenseError;
        private set => SetProperty(ref _deletingExpenseError, value);
    }

    public RelayCommand<string> ShowCreateExpenseFormCommand { get; private set; } = null!;
    public RelayCommand CancelExpenseFormCommand { get; private set; } = null!;
    public RelayCommand SaveExpenseCommand { get; private set; } = null!;
    public RelayCommand<ExpenseDTO> EditExpenseCommand { get; private set; } = null!;
    public RelayCommand<ExpenseDTO> DeleteExpenseCommand { get; private set; } = null!;

    private void InitializeExpenseCommands()
    {
        ShowCreateExpenseFormCommand = new RelayCommand<string>(ShowCreateExpenseForm);
        CancelExpenseFormCommand = new RelayCommand(CloseExpenseForm);
        SaveExpenseCommand = new RelayCommand(async () => await SaveExpenseAsync(), () => !IsSavingExpense);
        EditExpenseCommand = new RelayCommand<ExpenseDTO>(ShowEditExpenseForm);
        DeleteExpenseCommand = new RelayCommand<ExpenseDTO>(
            async expense => await DeleteExpenseAsync(expense),
            expense => expense?.PaymentStatus != SettledStatus);
    }

    private void ShowCreateExpenseForm(string? mode)
    {
        _editingExpenseId = null;
        ExpenseFormDate = DateTime.Today;
        ExpenseFormDescription = string.Empty;
        ExpenseFormCategoryId = ActiveCategories.FirstOrDefault()?.Id;
        ExpenseFormValue = string.Empty;
        IsCardPaymentMode = mode == "card";
        ExpenseFormPaymentSource = IsCardPaymentMode
            ? null
            : (Banks.Count > 0 ? Banks[0].Id : null);
        ExpenseFormCreditCardId = null;
        ExpenseFormCreditCardName = string.Empty;
        ExpenseFormRoundUpAmount = string.Empty;
        ExpenseFormCountsAsTithe = true;
        ExpenseFormIsSettled = false;
        _invoiceDateTouchedByUser = false;
        if (IsCardPaymentMode)
        {
            SetDefaultInvoiceDate(ExpenseFormDate!.Value.Year, ExpenseFormDate.Value.Month);
        }
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
        ExpenseFormRoundUpAmount = expense.RoundUpAmount?.ToString("0.##") ?? string.Empty;
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

            CloseExpenseForm();
            await RefreshAsync();
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
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            DeletingExpenseError = ex.Message;
        }
    }

    public const string AllBanksFilter = "All Banks";

    private string? _bankOperationsError;
    private string _selectedBankFilter = AllBanksFilter;
    private IReadOnlyList<string> _bankFilterOptions = [AllBanksFilter];

    public ObservableCollection<BankOperationRow> BankOperations { get; } = [];
    public ObservableCollection<BankOperationRow> FilteredBankOperations { get; } = [];

    public bool HasBankOperations => FilteredBankOperations.Count > 0;

    public string BankOperationsEmptyMessage => SelectedBankFilter == AllBanksFilter
        ? "No transfers or balance corrections this month."
        : $"No transfers or balance corrections for {SelectedBankFilter} this month.";

    public string? BankOperationsError
    {
        get => _bankOperationsError;
        private set => SetProperty(ref _bankOperationsError, value);
    }

    public string SelectedBankFilter
    {
        get => _selectedBankFilter;
        set
        {
            if (SetProperty(ref _selectedBankFilter, value))
            {
                OnPropertyChanged(nameof(BankOperationsEmptyMessage));
                ApplyBankFilter();
            }
        }
    }

    public IReadOnlyList<string> BankFilterOptions
    {
        get => _bankFilterOptions;
        private set => SetProperty(ref _bankFilterOptions, value);
    }

    public RelayCommand<BankOperationRow> DeleteBankOperationCommand { get; private set; } = null!;

    private void InitializeBankCommands()
    {
        DeleteBankOperationCommand = new RelayCommand<BankOperationRow>(async row => await DeleteBankOperationAsync(row));
    }

    private void ApplyBankFilter()
    {
        var matching = SelectedBankFilter == AllBanksFilter
            ? BankOperations
            : BankOperations.Where(row => row.MatchesBank(SelectedBankFilter));

        ReplaceAll(FilteredBankOperations, matching);
        OnPropertyChanged(nameof(HasBankOperations));
    }

    internal async Task DeleteBankOperationAsync(BankOperationRow? row)
    {
        if (row is null)
        {
            return;
        }

        var confirmMessage = row.Kind == BankOperationKind.Adjustment
            ? "Delete this balance adjustment? This removes it for good."
            : "Delete this transfer? This removes it for good.";

        if (!_confirm(confirmMessage))
        {
            return;
        }

        BankOperationsError = null;

        try
        {
            if (row.Transfer is { } transfer)
            {
                await _transferService.DeleteTransferAsync(transfer.Id);
            }
            else if (row.Adjustment is { } adjustment)
            {
                await _balanceAdjustmentService.DeleteAdjustmentAsync(adjustment.BankId, adjustment.Id);
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            BankOperationsError = ex.Message;
        }
    }

    private bool _isTransferFormOpen;
    private Guid? _editingTransferId;
    private DateTime? _transferFormDate;
    private Guid? _transferFormSourceBank;
    private Guid? _transferFormDestinationBank;
    private string _transferFormAmount = string.Empty;
    private string _transferFormNote = string.Empty;
    private bool _isSavingTransfer;
    private string? _transferSaveError;

    public bool IsTransferFormOpen
    {
        get => _isTransferFormOpen;
        private set => SetProperty(ref _isTransferFormOpen, value);
    }

    public bool IsEditingTransfer => _editingTransferId != null;

    public DateTime? TransferFormDate
    {
        get => _transferFormDate;
        set => SetProperty(ref _transferFormDate, value);
    }

    public Guid? TransferFormSourceBank
    {
        get => _transferFormSourceBank;
        set
        {
            if (SetProperty(ref _transferFormSourceBank, value))
            {
                OnPropertyChanged(nameof(IsSameBankTransfer));
                OnPropertyChanged(nameof(SameBankTransferError));
                SaveTransferCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public Guid? TransferFormDestinationBank
    {
        get => _transferFormDestinationBank;
        set
        {
            if (SetProperty(ref _transferFormDestinationBank, value))
            {
                OnPropertyChanged(nameof(IsSameBankTransfer));
                OnPropertyChanged(nameof(SameBankTransferError));
                SaveTransferCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>True when source and destination are both set and identical — Move Money's Confirm is disabled in this state, mirroring TransferForm.tsx's sameBankError.</summary>
    public bool IsSameBankTransfer =>
        TransferFormSourceBank.HasValue
        && TransferFormDestinationBank.HasValue
        && TransferFormSourceBank == TransferFormDestinationBank;

    public string SameBankTransferError => IsSameBankTransfer ? "Source and destination must be different banks." : string.Empty;

    public string TransferFormAmount
    {
        get => _transferFormAmount;
        set => SetProperty(ref _transferFormAmount, value);
    }

    public string TransferFormNote
    {
        get => _transferFormNote;
        set => SetProperty(ref _transferFormNote, value);
    }

    public bool IsSavingTransfer
    {
        get => _isSavingTransfer;
        private set => SetProperty(ref _isSavingTransfer, value);
    }

    public string? TransferSaveError
    {
        get => _transferSaveError;
        private set => SetProperty(ref _transferSaveError, value);
    }

    public RelayCommand<Guid?> ShowMoveMoneyFormCommand { get; private set; } = null!;
    public RelayCommand CancelTransferFormCommand { get; private set; } = null!;
    public RelayCommand SaveTransferCommand { get; private set; } = null!;
    public RelayCommand<TransferDTO> EditTransferCommand { get; private set; } = null!;

    private void InitializeTransferCommands()
    {
        ShowMoveMoneyFormCommand = new RelayCommand<Guid?>(ShowCreateTransferForm);
        CancelTransferFormCommand = new RelayCommand(CloseTransferForm);
        SaveTransferCommand = new RelayCommand(async () => await SaveTransferAsync(), () => !IsSavingTransfer && !IsSameBankTransfer);
        EditTransferCommand = new RelayCommand<TransferDTO>(ShowEditTransferForm);
    }

    private void ShowCreateTransferForm(Guid? sourceBank)
    {
        _editingTransferId = null;
        TransferFormDate = DateTime.Today;
        TransferFormSourceBank = sourceBank ?? (Banks.Count > 0 ? Banks[0].Id : null);
        TransferFormDestinationBank = null;
        TransferFormAmount = string.Empty;
        TransferFormNote = string.Empty;
        TransferSaveError = null;
        OnPropertyChanged(nameof(IsEditingTransfer));
        IsTransferFormOpen = true;
    }

    private void ShowEditTransferForm(TransferDTO? transfer)
    {
        if (transfer is null)
        {
            return;
        }

        _editingTransferId = transfer.Id;
        TransferFormDate = transfer.Date.ToDateTime(TimeOnly.MinValue);
        TransferFormSourceBank = transfer.SourceBankId;
        TransferFormDestinationBank = transfer.DestinationBankId;
        TransferFormAmount = transfer.Amount.ToString("0.##");
        TransferFormNote = transfer.Note ?? string.Empty;
        TransferSaveError = null;
        OnPropertyChanged(nameof(IsEditingTransfer));
        IsTransferFormOpen = true;
    }

    private void CloseTransferForm()
    {
        IsTransferFormOpen = false;
        _editingTransferId = null;
        TransferSaveError = null;
    }

    internal Task SaveTransferAsync() => ExecuteSaveAsync(
        () => TransferFormValidation.BuildValidationMessage(
            TransferFormDate, TransferFormSourceBank, TransferFormDestinationBank, TransferFormAmount),
        error => TransferSaveError = error,
        saving => IsSavingTransfer = saving,
        async () =>
        {
            var date = DateOnly.FromDateTime(TransferFormDate!.Value);
            var amount = decimal.Parse(TransferFormAmount);
            var note = string.IsNullOrWhiteSpace(TransferFormNote) ? null : TransferFormNote;

            if (_editingTransferId is { } id)
            {
                await _transferService.UpdateTransferAsync(id, new TransferUpdateDTO
                {
                    Date = date, SourceBankId = TransferFormSourceBank!.Value,
                    DestinationBankId = TransferFormDestinationBank!.Value,
                    Amount = amount, Note = note,
                });
            }
            else
            {
                await _transferService.AddTransferAsync(new TransferCreateDTO
                {
                    Date = date, SourceBankId = TransferFormSourceBank!.Value,
                    DestinationBankId = TransferFormDestinationBank!.Value,
                    Amount = amount, Note = note,
                });
            }

            CloseTransferForm();
            await RefreshAsync();
        },
        SaveTransferCommand.RaiseCanExecuteChanged);

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
                AdjustmentFormCurrentBalance = BankTotals.FirstOrDefault(b => b.BankId == value)?.Balance ?? 0m;
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
        private set => SetProperty(ref _adjustmentSaveError, value);
    }

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

    public RelayCommand ShowCorrectBalanceFormCommand { get; private set; } = null!;
    public RelayCommand<BalanceAdjustmentDTO> EditAdjustmentCommand { get; private set; } = null!;
    public RelayCommand CancelAdjustmentFormCommand { get; private set; } = null!;
    public RelayCommand SaveAdjustmentCommand { get; private set; } = null!;
    public RelayCommand DismissAdjustmentResultCommand { get; private set; } = null!;

    private void InitializeAdjustmentCommands()
    {
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
        AdjustmentFormDate = DateTime.Today;
        AdjustmentFormTargetBalance = string.Empty;
        AdjustmentFormNote = string.Empty;
        AdjustmentSaveError = null;
        AdjustmentSavedDelta = null;
        AdjustmentFormBankName = null;
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

            await RefreshAsync();
            AdjustmentSavedDelta = result.Delta;
        },
        SaveAdjustmentCommand.RaiseCanExecuteChanged);

    private string? _cardStatementError;

    public string? CardStatementError
    {
        get => _cardStatementError;
        private set => SetProperty(ref _cardStatementError, value);
    }

    private string? _cardStatementWarning;

    /// <summary>
    /// A completed call that changed nothing - marking a statement paid that already was, say.
    /// Separate from <see cref="CardStatementError"/> because it is not a failure, and showing it
    /// in red would teach the user to ignore red. Mirrors useMonthly.ts's listActionWarning.
    /// </summary>
    public string? CardStatementWarning
    {
        get => _cardStatementWarning;
        private set => SetProperty(ref _cardStatementWarning, value);
    }

    public Dictionary<Guid, Guid> MarkPaidSources { get; } = [];

    public RelayCommand<CardStatementDTO> MarkStatementPaidCommand { get; private set; } = null!;
    public RelayCommand<CardStatementDTO> UnmarkStatementPaidCommand { get; private set; } = null!;

    private void InitializeCardCommands()
    {
        MarkStatementPaidCommand = new RelayCommand<CardStatementDTO>(
            async statement => await MarkStatementPaidAsync(statement),
            statement => statement != null && MarkPaidSources.ContainsKey(statement.Id));
        UnmarkStatementPaidCommand = new RelayCommand<CardStatementDTO>(async statement => await UnmarkStatementPaidAsync(statement));
    }

    public void SetMarkPaidSource(Guid statementId, Guid bankId)
    {
        MarkPaidSources[statementId] = bankId;
        MarkStatementPaidCommand.RaiseCanExecuteChanged();
    }

    internal async Task MarkStatementPaidAsync(CardStatementDTO? statement)
    {
        if (statement is null || !MarkPaidSources.TryGetValue(statement.Id, out var paymentSource))
        {
            return;
        }

        CardStatementError = null;
        CardStatementWarning = null;

        try
        {
            var result = await _cardStatementService.MarkStatementPaidAsync(statement.Id, new MarkStatementPaidDTO { PaymentSourceBankId = paymentSource });
            CardStatementWarning = result.Warning;
            MarkPaidSources.Remove(statement.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            CardStatementError = ex.Message;
        }
    }

    internal async Task UnmarkStatementPaidAsync(CardStatementDTO? statement)
    {
        if (statement is null)
        {
            return;
        }

        CardStatementError = null;
        CardStatementWarning = null;

        try
        {
            var result = await _cardStatementService.UnmarkStatementPaidAsync(statement.Id);
            CardStatementWarning = result.Warning;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            CardStatementError = ex.Message;
        }
    }

    private string? _creditCardUpdateError;
    private Guid? _updatingCreditCardId;

    public string? CreditCardUpdateError
    {
        get => _creditCardUpdateError;
        private set => SetProperty(ref _creditCardUpdateError, value);
    }

    public Guid? UpdatingCreditCardId
    {
        get => _updatingCreditCardId;
        private set => SetProperty(ref _updatingCreditCardId, value);
    }

    internal async Task UpdateCreditCardAsync(CreditCardDTO? card, DateOnly? nextInvoiceDueDate, bool isActive)
    {
        if (card is null)
        {
            return;
        }

        // The grid's DatePicker/CheckBox bind one-way to this row and call back here on their
        // change events - but WPF raises those same events when ReplaceAll(CreditCards, ...)
        // below rebinds the row to its own current value, not just on a real user edit. Without
        // this guard, that echo would call the update service and refresh again, which rebinds
        // the row again, forever.
        if (card.NextInvoiceDueDate == nextInvoiceDueDate && card.IsActive == isActive)
        {
            return;
        }

        CreditCardUpdateError = null;
        UpdatingCreditCardId = card.Id;

        try
        {
            await _creditCardService.UpdateCreditCardAsync(card.Id, new CreditCardUpdateDTO
            {
                NextInvoiceDueDate = nextInvoiceDueDate,
                IsActive = isActive,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            CreditCardUpdateError = ex.Message;
        }
        finally
        {
            UpdatingCreditCardId = null;
        }
    }
}
