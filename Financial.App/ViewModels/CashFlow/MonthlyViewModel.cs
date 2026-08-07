using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// ViewModel for the Monthly tab: owns the selected period and the data/forms for its
/// Summary (category totals, tithe), Expense, and Incoming sub-tabs. Mirrors Financial.Web's
/// useMonthly.ts hook. F03 extends this same class with Banks/Cards/Transfer/Adjustment
/// state rather than introducing a second Monthly ViewModel.
/// </summary>
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
    public ObservableCollection<IncomeDTO> Incomes { get; } = [];
    public ObservableCollection<CategoryTotalDTO> CategoryTotals { get; } = [];
    public ObservableCollection<BankDTO> Banks { get; } = [];
    public ObservableCollection<IncomeSourceDTO> IncomeSources { get; } = [];
    public ObservableCollection<BankTotalRow> BankTotals { get; } = [];
    public ObservableCollection<CardStatementDTO> CardStatements { get; } = [];
    public ObservableCollection<IncomeTotalRow> IncomeTotals { get; } = [];

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
        Func<string, bool> confirm)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _incomeSourceService = incomeSourceService ?? throw new ArgumentNullException(nameof(incomeSourceService));
        _titheService = titheService ?? throw new ArgumentNullException(nameof(titheService));
        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
        _balanceAdjustmentService = balanceAdjustmentService ?? throw new ArgumentNullException(nameof(balanceAdjustmentService));
        _cardStatementService = cardStatementService ?? throw new ArgumentNullException(nameof(cardStatementService));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));

        var today = DateTime.Today;
        _year = today.Year;
        _month = today.Month;

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        InitializeExpenseCommands();
        InitializeIncomeCommands();
        InitializeBankCommands();
        InitializeTransferCommands();
        InitializeAdjustmentCommands();
        InitializeCardCommands();

        _ = RefreshAsync();
    }

    private int _refreshRequestId;

    /// <summary>
    /// Reloads expenses/incomes/category totals/banks/tithe for the selected period. Guards
    /// against overlapping calls (e.g. the constructor's initial load racing a rapid year/month
    /// change or a manual retry) by discarding a completion whose request has been superseded.
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

            var expenses = await Task.Run(() => _expenseService.GetExpensesByMonth(year, month));
            var unpaidCardCharges = await Task.Run(() => _expenseService.GetUnpaidCardChargesByMonth(year, month));
            var incomes = await Task.Run(() => _incomeService.GetIncomesByMonth(year, month));
            var categoryTotals = await Task.Run(() => _expenseService.GetCategoryTotalsByMonth(year, month));
            var banks = await Task.Run(() => _bankService.GetBanks());
            var incomeSources = await Task.Run(() => _incomeSourceService.GetIncomeSources());
            var bankBalances = await Task.Run(() => _bankService.GetBankBalancesByMonth(year, month));
            var titheSummary = await Task.Run(() => _titheService.GetTitheSummary(year, month));
            var transfers = await Task.Run(() => _transferService.GetTransfersByMonth(year, month));
            var adjustmentsByBank = new Dictionary<string, IReadOnlyList<BalanceAdjustmentDTO>>();
            foreach (var bank in banks)
            {
                adjustmentsByBank[bank.Name] = await Task.Run(() => _balanceAdjustmentService.GetAdjustmentsByBank(bank.Id));
            }
            var cardStatements = await _cardStatementService.GetStatementsForMonthAsync(year, month);

            if (requestId != _refreshRequestId)
            {
                return;
            }

            ReplaceAll(Expenses, expenses);
            ReplaceAll(UnpaidCardCharges, unpaidCardCharges);
            ReplaceAll(Incomes, incomes);
            ReplaceAll(CategoryTotals, categoryTotals);
            ReplaceAll(Banks, banks);
            ReplaceAll(IncomeSources, incomeSources);
            OnPropertyChanged(nameof(IncomeSourceOptions));
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

    private static void ReplaceAll<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>Resolves a bank name from a name-bound form field to its identifier. Forms remain name-based until F06 introduces Id-based picklists.</summary>
    private Guid? ResolveBankId(string? bankName) =>
        string.IsNullOrWhiteSpace(bankName) ? null : Banks.FirstOrDefault(b => b.Name == bankName)?.Id;

    /// <summary>Resolves an income source name from a name-bound form field to its identifier.</summary>
    private Guid ResolveIncomeSourceId(string incomeSourceName) =>
        IncomeSources.FirstOrDefault(s => s.Name == incomeSourceName)?.Id ?? Guid.Empty;

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
            return new BankTotalRow { Bank = bank.Name, Balance = balance, RoundUpTotal = roundUpTotal };
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

    /// <summary>Combines every bank's transfers and adjustments for the month into one flat, newest-first list, mirroring the Bank tab's useMonthly.ts equivalent.</summary>
    private static List<BankOperationRow> BuildBankOperations(
        IReadOnlyList<TransferDTO> transfers,
        IReadOnlyDictionary<string, IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank,
        int year,
        int month)
    {
        // Transfers are already month-scoped by ITransferService.GetTransfersByMonth; only
        // adjustments (fetched per bank via GetAdjustmentsByBank, not month-scoped) need filtering here.
        var rows = new List<BankOperationRow>(transfers.Select(BankOperationRow.FromTransfer));

        rows.AddRange(adjustmentsByBank.Values
            .SelectMany(adjustments => adjustments)
            .Where(a => a.Date.Year == year && a.Date.Month == month)
            .Select(BankOperationRow.FromAdjustment));

        return rows.OrderByDescending(r => r.Date).ToList();
    }

    /// <summary>Options for the Bank tab's filter dropdown: "All Banks" plus each configured bank name.</summary>
    private static IReadOnlyList<string> BuildBankFilterOptions(IReadOnlyList<BankDTO> banks) =>
        new[] { AllBanksFilter }.Concat(banks.Select(b => b.Name)).ToList();

    // ----- Expense CRUD -----

    private const string SettledStatus = "CreditCardSettled";

    public static readonly IReadOnlyList<string> Categories =
    [
        "Ariana", "Carro", "Casa", "Estudo", "Extras", "Familia", "Gleison",
        "Mercado", "Samuel", "Saude", "Viagem", "Dizimo", "Investimento", "Reserva",
    ];

    public static readonly IReadOnlyList<string> Cards =
    [
        "BarclaysPlatinumVisa8003", "BarclaysPlatinumVisa6007", "ChaseMaster4023", "BaAmex", "PaypalCredit",
    ];

    /// <summary>Instance-level accessor for <see cref="Categories"/> — WPF's Binding only resolves instance members, not static fields.</summary>
    public IReadOnlyList<string> CategoryOptions => Categories;

    /// <summary>Instance-level accessor for <see cref="Cards"/> — WPF's Binding only resolves instance members, not static fields.</summary>
    public IReadOnlyList<string> CardOptions => Cards;

    private bool _isExpenseFormOpen;
    private Guid? _editingExpenseId;
    private DateTime? _expenseFormDate;
    private string _expenseFormDescription = string.Empty;
    private string _expenseFormCategory = Categories[0];
    private string _expenseFormValue = string.Empty;
    private bool _isCardPaymentMode;
    private string _expenseFormPaymentSource = string.Empty;
    private string _expenseFormCardTag = string.Empty;
    private string _expenseFormRoundUpAmount = string.Empty;
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

    public string ExpenseFormCategory
    {
        get => _expenseFormCategory;
        set => SetProperty(ref _expenseFormCategory, value);
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

    public string ExpenseFormPaymentSource
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

    public string ExpenseFormCardTag
    {
        get => _expenseFormCardTag;
        set => SetProperty(ref _expenseFormCardTag, value);
    }

    public string ExpenseFormRoundUpAmount
    {
        get => _expenseFormRoundUpAmount;
        set => SetProperty(ref _expenseFormRoundUpAmount, value);
    }

    public bool ShowRoundUpField =>
        !IsCardPaymentMode
        && Banks.FirstOrDefault(b => b.Name == ExpenseFormPaymentSource) is { RoundUpEnabled: true };

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
        ExpenseFormCategory = Categories[0];
        ExpenseFormValue = string.Empty;
        IsCardPaymentMode = mode == "card";
        ExpenseFormPaymentSource = IsCardPaymentMode
            ? string.Empty
            : (Banks.Count > 0 ? Banks[0].Name : string.Empty);
        ExpenseFormCardTag = string.Empty;
        ExpenseFormRoundUpAmount = string.Empty;
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
        ExpenseFormCategory = expense.Category;
        ExpenseFormValue = expense.Value.ToString("0.##");
        IsCardPaymentMode = expense.CardTag != null;
        ExpenseFormPaymentSource = expense.PaymentSourceBankName ?? string.Empty;
        ExpenseFormCardTag = expense.CardTag ?? string.Empty;
        ExpenseFormRoundUpAmount = expense.RoundUpAmount?.ToString("0.##") ?? string.Empty;
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
        var validationMessage = ExpenseFormValidation.BuildValidationMessage(
            ExpenseFormDate, ExpenseFormDescription, ExpenseFormCategory, ExpenseFormValue,
            IsCardPaymentMode, ExpenseFormPaymentSource, ExpenseFormCardTag, ShowRoundUpField, ExpenseFormRoundUpAmount);

        if (!string.IsNullOrEmpty(validationMessage))
        {
            ExpenseSaveError = validationMessage;
            return;
        }

        IsSavingExpense = true;
        SaveExpenseCommand.RaiseCanExecuteChanged();
        ExpenseSaveError = null;

        try
        {
            var date = DateOnly.FromDateTime(ExpenseFormDate!.Value);
            var value = decimal.Parse(ExpenseFormValue);
            var paymentSource = IsCardPaymentMode ? null : ExpenseFormPaymentSource;
            var cardTag = IsCardPaymentMode ? ExpenseFormCardTag : null;
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
                    Category = ExpenseFormCategory,
                    PaymentSourceBankId = ResolveBankId(paymentSource),
                    CardTag = cardTag,
                    InvoiceDate = invoiceDate,
                    RoundUpAmount = roundUpAmount,
                });
            }
            else
            {
                await _expenseService.AddExpenseAsync(new ExpenseCreateDTO
                {
                    Date = date,
                    Description = ExpenseFormDescription,
                    Value = value,
                    Category = ExpenseFormCategory,
                    PaymentSourceBankId = ResolveBankId(paymentSource),
                    CardTag = cardTag,
                    InvoiceDate = invoiceDate,
                    RoundUpAmount = roundUpAmount,
                });
            }

            CloseExpenseForm();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ExpenseSaveError = ex.Message;
        }
        finally
        {
            IsSavingExpense = false;
            SaveExpenseCommand.RaiseCanExecuteChanged();
        }
    }

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

    // ----- Income CRUD -----

    private static readonly HashSet<string> IncomeSourcesWithGrossValue = ["Gleison", "Ariana"];

    /// <summary>Matches the picklist's historical display order. A source name outside this list
    /// (unexpected but not invalid) sorts last rather than being dropped or erroring.</summary>
    private static readonly string[] IncomeSourceDisplayOrder = ["Gleison", "Ariana", "Lottery", "DividendoJuros"];

    /// <summary>Active income sources from <see cref="IncomeSources"/>, ordered to match the picklist's historical display order.</summary>
    public IReadOnlyList<string> IncomeSourceOptions =>
        IncomeSources
            .Where(s => s.IsActive)
            .OrderBy(s => IncomeSourceRank(s.Name))
            .Select(s => s.Name)
            .ToList();

    private static int IncomeSourceRank(string name)
    {
        var index = Array.IndexOf(IncomeSourceDisplayOrder, name);
        return index == -1 ? IncomeSourceDisplayOrder.Length : index;
    }

    private bool _isIncomeFormOpen;
    private Guid? _editingIncomeId;
    private DateTime? _incomeFormDate;
    private string _incomeFormSource = string.Empty;
    private string _incomeFormGrossValue = string.Empty;
    private string _incomeFormNetValue = string.Empty;
    private string _incomeFormBank = string.Empty;
    private bool _isSavingIncome;
    private string? _incomeSaveError;
    private string? _deletingIncomeError;

    public bool IsIncomeFormOpen
    {
        get => _isIncomeFormOpen;
        private set => SetProperty(ref _isIncomeFormOpen, value);
    }

    public bool IsEditingIncome => _editingIncomeId != null;

    public DateTime? IncomeFormDate
    {
        get => _incomeFormDate;
        set => SetProperty(ref _incomeFormDate, value);
    }

    public string IncomeFormSource
    {
        get => _incomeFormSource;
        set
        {
            if (SetProperty(ref _incomeFormSource, value))
            {
                OnPropertyChanged(nameof(ShowIncomeGrossValueField));
            }
        }
    }

    public bool ShowIncomeGrossValueField => IncomeSourcesWithGrossValue.Contains(IncomeFormSource);

    public string IncomeFormGrossValue
    {
        get => _incomeFormGrossValue;
        set => SetProperty(ref _incomeFormGrossValue, value);
    }

    public string IncomeFormNetValue
    {
        get => _incomeFormNetValue;
        set => SetProperty(ref _incomeFormNetValue, value);
    }

    public string IncomeFormBank
    {
        get => _incomeFormBank;
        set => SetProperty(ref _incomeFormBank, value);
    }

    public bool IsSavingIncome
    {
        get => _isSavingIncome;
        private set => SetProperty(ref _isSavingIncome, value);
    }

    public string? IncomeSaveError
    {
        get => _incomeSaveError;
        private set => SetProperty(ref _incomeSaveError, value);
    }

    public string? DeletingIncomeError
    {
        get => _deletingIncomeError;
        private set => SetProperty(ref _deletingIncomeError, value);
    }

    public RelayCommand ShowCreateIncomeFormCommand { get; private set; } = null!;
    public RelayCommand CancelIncomeFormCommand { get; private set; } = null!;
    public RelayCommand SaveIncomeCommand { get; private set; } = null!;
    public RelayCommand<IncomeDTO> EditIncomeCommand { get; private set; } = null!;
    public RelayCommand<IncomeDTO> DeleteIncomeCommand { get; private set; } = null!;

    private void InitializeIncomeCommands()
    {
        ShowCreateIncomeFormCommand = new RelayCommand(ShowCreateIncomeForm);
        CancelIncomeFormCommand = new RelayCommand(CloseIncomeForm);
        SaveIncomeCommand = new RelayCommand(async () => await SaveIncomeAsync(), () => !IsSavingIncome);
        EditIncomeCommand = new RelayCommand<IncomeDTO>(ShowEditIncomeForm);
        DeleteIncomeCommand = new RelayCommand<IncomeDTO>(async income => await DeleteIncomeAsync(income));
    }

    private void ShowCreateIncomeForm()
    {
        _editingIncomeId = null;
        IncomeFormDate = DateTime.Today;
        IncomeFormSource = IncomeSourceOptions.Count > 0 ? IncomeSourceOptions[0] : string.Empty;
        IncomeFormGrossValue = string.Empty;
        IncomeFormNetValue = string.Empty;
        IncomeFormBank = Banks.Count > 0 ? Banks[0].Name : string.Empty;
        IncomeSaveError = null;
        OnPropertyChanged(nameof(IsEditingIncome));
        IsIncomeFormOpen = true;
    }

    private void ShowEditIncomeForm(IncomeDTO? income)
    {
        if (income is null)
        {
            return;
        }

        _editingIncomeId = income.Id;
        IncomeFormDate = income.Date.ToDateTime(TimeOnly.MinValue);
        IncomeFormSource = income.IncomeSourceName;
        IncomeFormGrossValue = income.GrossValue?.ToString("0.##") ?? string.Empty;
        IncomeFormNetValue = income.NetValue.ToString("0.##");
        IncomeFormBank = income.BankName;
        IncomeSaveError = null;
        OnPropertyChanged(nameof(IsEditingIncome));
        IsIncomeFormOpen = true;
    }

    private void CloseIncomeForm()
    {
        IsIncomeFormOpen = false;
        _editingIncomeId = null;
        IncomeSaveError = null;
    }

    internal async Task SaveIncomeAsync()
    {
        var validationMessage = IncomeFormValidation.BuildValidationMessage(
            IncomeFormDate, IncomeFormSource, IncomeFormNetValue, IncomeFormBank);

        if (!string.IsNullOrEmpty(validationMessage))
        {
            IncomeSaveError = validationMessage;
            return;
        }

        IsSavingIncome = true;
        SaveIncomeCommand.RaiseCanExecuteChanged();
        IncomeSaveError = null;

        try
        {
            var date = DateOnly.FromDateTime(IncomeFormDate!.Value);
            var netValue = decimal.Parse(IncomeFormNetValue);
            decimal? grossValue = ShowIncomeGrossValueField && decimal.TryParse(IncomeFormGrossValue, out var parsedGross)
                ? parsedGross
                : null;

            if (_editingIncomeId is { } id)
            {
                await _incomeService.UpdateIncomeAsync(id, new IncomeUpdateDTO
                {
                    Date = date,
                    IncomeSourceId = ResolveIncomeSourceId(IncomeFormSource),
                    GrossValue = grossValue,
                    NetValue = netValue,
                    BankId = ResolveBankId(IncomeFormBank) ?? Guid.Empty,
                });
            }
            else
            {
                await _incomeService.AddIncomeAsync(new IncomeCreateDTO
                {
                    Date = date,
                    IncomeSourceId = ResolveIncomeSourceId(IncomeFormSource),
                    GrossValue = grossValue,
                    NetValue = netValue,
                    BankId = ResolveBankId(IncomeFormBank) ?? Guid.Empty,
                });
            }

            CloseIncomeForm();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            IncomeSaveError = ex.Message;
        }
        finally
        {
            IsSavingIncome = false;
            SaveIncomeCommand.RaiseCanExecuteChanged();
        }
    }

    internal async Task DeleteIncomeAsync(IncomeDTO? income)
    {
        if (income is null)
        {
            return;
        }

        if (!_confirm($"Delete this income entry from {income.IncomeSourceName}? This removes it for good."))
        {
            return;
        }

        DeletingIncomeError = null;

        try
        {
            await _incomeService.DeleteIncomeAsync(income.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            DeletingIncomeError = ex.Message;
        }
    }

    // ----- Bank tab: flat operations list & filter -----

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

    /// <summary>Recomputes <see cref="FilteredBankOperations"/> from the already-fetched <see cref="BankOperations"/> — no network request.</summary>
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

    // ----- Transfer form -----

    private bool _isTransferFormOpen;
    private Guid? _editingTransferId;
    private DateTime? _transferFormDate;
    private string _transferFormSourceBank = string.Empty;
    private string _transferFormDestinationBank = string.Empty;
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

    public string TransferFormSourceBank
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

    public string TransferFormDestinationBank
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
        !string.IsNullOrEmpty(TransferFormSourceBank)
        && !string.IsNullOrEmpty(TransferFormDestinationBank)
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

    public RelayCommand<string> ShowMoveMoneyFormCommand { get; private set; } = null!;
    public RelayCommand CancelTransferFormCommand { get; private set; } = null!;
    public RelayCommand SaveTransferCommand { get; private set; } = null!;
    public RelayCommand<TransferDTO> EditTransferCommand { get; private set; } = null!;

    private void InitializeTransferCommands()
    {
        ShowMoveMoneyFormCommand = new RelayCommand<string>(ShowCreateTransferForm);
        CancelTransferFormCommand = new RelayCommand(CloseTransferForm);
        SaveTransferCommand = new RelayCommand(async () => await SaveTransferAsync(), () => !IsSavingTransfer && !IsSameBankTransfer);
        EditTransferCommand = new RelayCommand<TransferDTO>(ShowEditTransferForm);
    }

    private void ShowCreateTransferForm(string? sourceBank)
    {
        _editingTransferId = null;
        TransferFormDate = DateTime.Today;
        TransferFormSourceBank = sourceBank ?? (Banks.Count > 0 ? Banks[0].Name : string.Empty);
        TransferFormDestinationBank = string.Empty;
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
        TransferFormSourceBank = transfer.SourceBankName;
        TransferFormDestinationBank = transfer.DestinationBankName;
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

    internal async Task SaveTransferAsync()
    {
        var validationMessage = TransferFormValidation.BuildValidationMessage(
            TransferFormDate, TransferFormSourceBank, TransferFormDestinationBank, TransferFormAmount);

        if (!string.IsNullOrEmpty(validationMessage))
        {
            TransferSaveError = validationMessage;
            return;
        }

        IsSavingTransfer = true;
        SaveTransferCommand.RaiseCanExecuteChanged();
        TransferSaveError = null;

        try
        {
            var date = DateOnly.FromDateTime(TransferFormDate!.Value);
            var amount = decimal.Parse(TransferFormAmount);
            var note = string.IsNullOrWhiteSpace(TransferFormNote) ? null : TransferFormNote;

            if (_editingTransferId is { } id)
            {
                await _transferService.UpdateTransferAsync(id, new TransferUpdateDTO
                {
                    Date = date, SourceBankId = ResolveBankId(TransferFormSourceBank) ?? Guid.Empty,
                    DestinationBankId = ResolveBankId(TransferFormDestinationBank) ?? Guid.Empty,
                    Amount = amount, Note = note,
                });
            }
            else
            {
                await _transferService.AddTransferAsync(new TransferCreateDTO
                {
                    Date = date, SourceBankId = ResolveBankId(TransferFormSourceBank) ?? Guid.Empty,
                    DestinationBankId = ResolveBankId(TransferFormDestinationBank) ?? Guid.Empty,
                    Amount = amount, Note = note,
                });
            }

            CloseTransferForm();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            TransferSaveError = ex.Message;
        }
        finally
        {
            IsSavingTransfer = false;
            SaveTransferCommand.RaiseCanExecuteChanged();
        }
    }

    // ----- Balance Adjustment form -----

    private bool _isAdjustmentFormOpen;
    private Guid? _editingAdjustmentBank;
    private Guid? _editingAdjustmentId;
    private string _adjustmentFormBankName = string.Empty;
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

    /// <summary>
    /// The bank picked in the Correct Balance form. Setting it looks up that bank's already-loaded
    /// <see cref="BankTotals"/> balance (D3) — the same figure the Summary grid shows — and reveals
    /// the rest of the form via <see cref="IsAdjustmentBankSelected"/>.
    /// </summary>
    public string AdjustmentFormBankName
    {
        get => _adjustmentFormBankName;
        set
        {
            if (SetProperty(ref _adjustmentFormBankName, value))
            {
                AdjustmentFormCurrentBalance = BankTotals.FirstOrDefault(b => b.Bank == value)?.Balance ?? 0m;
                OnPropertyChanged(nameof(IsAdjustmentBankSelected));
                SaveAdjustmentCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Gates the rest of the Correct Balance form and its Save action until a bank has been picked.</summary>
    public bool IsAdjustmentBankSelected => !string.IsNullOrEmpty(AdjustmentFormBankName);

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

    /// <summary>Set after a successful save; the form shows this delta instead of the fields until dismissed.</summary>
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

    /// <summary>Generic entry point (D-per-spec Section 3/PRD F02): opens with no bank pre-selected.</summary>
    private void ShowCreateAdjustmentForm()
    {
        _editingAdjustmentBank = null;
        _editingAdjustmentId = null;
        AdjustmentFormDate = DateTime.Today;
        AdjustmentFormTargetBalance = string.Empty;
        AdjustmentFormNote = string.Empty;
        AdjustmentSaveError = null;
        AdjustmentSavedDelta = null;
        AdjustmentFormBankName = string.Empty;
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
        AdjustmentFormBankName = adjustment.BankName;
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

    internal async Task SaveAdjustmentAsync()
    {
        var validationMessage = BalanceAdjustmentFormValidation.BuildValidationMessage(AdjustmentFormDate, AdjustmentFormTargetBalance);

        if (!string.IsNullOrEmpty(validationMessage))
        {
            AdjustmentSaveError = validationMessage;
            return;
        }

        IsSavingAdjustment = true;
        SaveAdjustmentCommand.RaiseCanExecuteChanged();
        AdjustmentSaveError = null;

        try
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
                result = await _balanceAdjustmentService.AddAdjustmentAsync(ResolveBankId(AdjustmentFormBankName) ?? Guid.Empty, new BalanceAdjustmentCreateDTO
                {
                    Date = date, TargetBalance = targetBalance, Note = note,
                });
            }

            await RefreshAsync();
            AdjustmentSavedDelta = result.Delta;
        }
        catch (Exception ex)
        {
            AdjustmentSaveError = ex.Message;
        }
        finally
        {
            IsSavingAdjustment = false;
            SaveAdjustmentCommand.RaiseCanExecuteChanged();
        }
    }

    // ----- Cards grid -----

    private string? _cardStatementError;

    public string? CardStatementError
    {
        get => _cardStatementError;
        private set => SetProperty(ref _cardStatementError, value);
    }

    /// <summary>Pending "pay from" bank selection per unpaid card statement, keyed by statement id, mirroring useMonthly.ts's markPaidSources.</summary>
    public Dictionary<Guid, string> MarkPaidSources { get; } = [];

    public RelayCommand<CardStatementDTO> MarkStatementPaidCommand { get; private set; } = null!;
    public RelayCommand<CardStatementDTO> UnmarkStatementPaidCommand { get; private set; } = null!;

    private void InitializeCardCommands()
    {
        MarkStatementPaidCommand = new RelayCommand<CardStatementDTO>(
            async statement => await MarkStatementPaidAsync(statement),
            statement => statement != null && MarkPaidSources.ContainsKey(statement.Id));
        UnmarkStatementPaidCommand = new RelayCommand<CardStatementDTO>(async statement => await UnmarkStatementPaidAsync(statement));
    }

    public void SetMarkPaidSource(Guid statementId, string bankName)
    {
        MarkPaidSources[statementId] = bankName;
        MarkStatementPaidCommand.RaiseCanExecuteChanged();
    }

    internal async Task MarkStatementPaidAsync(CardStatementDTO? statement)
    {
        if (statement is null || !MarkPaidSources.TryGetValue(statement.Id, out var paymentSource))
        {
            return;
        }

        CardStatementError = null;

        try
        {
            await _cardStatementService.MarkStatementPaidAsync(statement.Id, new MarkStatementPaidDTO { PaymentSourceBankId = ResolveBankId(paymentSource) });
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

        try
        {
            await _cardStatementService.UnmarkStatementPaidAsync(statement.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            CardStatementError = ex.Message;
        }
    }
}
