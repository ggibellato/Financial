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
    public ObservableCollection<IncomeDTO> Incomes { get; } = [];
    public ObservableCollection<CategoryTotalDTO> CategoryTotals { get; } = [];
    public ObservableCollection<BankDTO> Banks { get; } = [];
    public ObservableCollection<BankTotalRow> BankTotals { get; } = [];
    public ObservableCollection<CardStatementDTO> CardStatements { get; } = [];

    public decimal BankTotalsSum => BankTotals.Sum(b => b.Balance);
    public decimal RoundUpTotalsSum => BankTotals.Sum(b => b.RoundUpTotal);
    public decimal AdjustmentTotal => CardStatements.Sum(s => s.OutstandingTotal);

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
        ITitheService titheService,
        ITransferService transferService,
        IBalanceAdjustmentService balanceAdjustmentService,
        ICardStatementService cardStatementService,
        Func<string, bool> confirm)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
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
            var incomes = await Task.Run(() => _incomeService.GetIncomesByMonth(year, month));
            var categoryTotals = await Task.Run(() => _expenseService.GetCategoryTotalsByMonth(year, month));
            var banks = await Task.Run(() => _bankService.GetBanks());
            var bankBalances = await Task.Run(() => _bankService.GetBankBalancesByMonth(year, month));
            var titheSummary = await Task.Run(() => _titheService.GetTitheSummary(year, month));
            var transfers = await Task.Run(() => _transferService.GetTransfersByMonth(year, month));
            var adjustmentsByBank = new Dictionary<string, IReadOnlyList<BalanceAdjustmentDTO>>();
            foreach (var bank in banks)
            {
                adjustmentsByBank[bank.Name] = await Task.Run(() => _balanceAdjustmentService.GetAdjustmentsByBank(bank.Name));
            }
            var cardStatements = await _cardStatementService.GetStatementsForMonthAsync(year, month);

            if (requestId != _refreshRequestId)
            {
                return;
            }

            ReplaceAll(Expenses, expenses);
            ReplaceAll(Incomes, incomes);
            ReplaceAll(CategoryTotals, categoryTotals);
            ReplaceAll(Banks, banks);
            TitheSummary = titheSummary;
            OnPropertyChanged(nameof(CategoryTotalsSum));

            var previouslyExpanded = BankTotals.Where(b => b.IsExpanded).Select(b => b.Bank).ToHashSet();
            var newBankTotals = BuildBankTotals(banks, expenses, bankBalances, transfers, adjustmentsByBank, year, month);
            foreach (var row in newBankTotals)
            {
                row.IsExpanded = previouslyExpanded.Contains(row.Bank);
            }
            ReplaceAll(BankTotals, newBankTotals);
            OnPropertyChanged(nameof(BankTotalsSum));
            OnPropertyChanged(nameof(RoundUpTotalsSum));

            ReplaceAll(CardStatements, cardStatements);
            OnPropertyChanged(nameof(AdjustmentTotal));
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

    /// <summary>Mirrors useMonthly.ts's bankTotals: balance from the month's running total, round-up summed client-side from that bank's expenses.</summary>
    private static List<BankTotalRow> BuildBankTotals(
        IReadOnlyList<BankDTO> banks,
        IReadOnlyList<ExpenseDTO> expenses,
        IReadOnlyList<BankBalanceDTO> bankBalances,
        IReadOnlyList<TransferDTO> transfers,
        IReadOnlyDictionary<string, IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank,
        int year,
        int month)
    {
        return banks.Select(bank =>
        {
            var roundUpTotal = expenses
                .Where(e => e.PaymentSource == bank.Name)
                .Sum(e => e.RoundUpAmount ?? 0m);
            var balance = bankBalances.FirstOrDefault(b => b.Bank == bank.Name)?.Balance ?? 0m;
            var row = new BankTotalRow { Bank = bank.Name, Balance = balance, RoundUpTotal = roundUpTotal };

            foreach (var entry in BuildBankHistory(bank.Name, transfers, adjustmentsByBank.GetValueOrDefault(bank.Name, []), year, month))
            {
                row.History.Add(entry);
            }

            return row;
        }).ToList();
    }

    /// <summary>Merges a bank's transfers and adjustments for the month into one history list, newest first, mirroring useBankHistory.ts.</summary>
    private static List<BankHistoryEntry> BuildBankHistory(
        string bankName, IReadOnlyList<TransferDTO> transfers, IReadOnlyList<BalanceAdjustmentDTO> adjustments, int year, int month)
    {
        var entries = new List<BankHistoryEntry>();

        foreach (var transfer in transfers)
        {
            if (transfer.SourceBank == bankName)
            {
                entries.Add(BankHistoryEntry.FromTransferOut(transfer));
            }
            else if (transfer.DestinationBank == bankName)
            {
                entries.Add(BankHistoryEntry.FromTransferIn(transfer));
            }
        }

        entries.AddRange(adjustments
            .Where(a => a.Date.Year == year && a.Date.Month == month)
            .Select(BankHistoryEntry.FromAdjustment));

        return entries.OrderByDescending(e => e.Date).ToList();
    }

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
        set => SetProperty(ref _expenseFormDate, value);
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

    public RelayCommand ShowCreateExpenseFormCommand { get; private set; } = null!;
    public RelayCommand CancelExpenseFormCommand { get; private set; } = null!;
    public RelayCommand SetBankPaymentModeCommand { get; private set; } = null!;
    public RelayCommand SetCardPaymentModeCommand { get; private set; } = null!;
    public RelayCommand SaveExpenseCommand { get; private set; } = null!;
    public RelayCommand<ExpenseDTO> EditExpenseCommand { get; private set; } = null!;
    public RelayCommand<ExpenseDTO> DeleteExpenseCommand { get; private set; } = null!;

    private void InitializeExpenseCommands()
    {
        ShowCreateExpenseFormCommand = new RelayCommand(ShowCreateExpenseForm);
        CancelExpenseFormCommand = new RelayCommand(CloseExpenseForm);
        SetBankPaymentModeCommand = new RelayCommand(() => IsCardPaymentMode = false);
        SetCardPaymentModeCommand = new RelayCommand(() => IsCardPaymentMode = true);
        SaveExpenseCommand = new RelayCommand(async () => await SaveExpenseAsync(), () => !IsSavingExpense);
        EditExpenseCommand = new RelayCommand<ExpenseDTO>(ShowEditExpenseForm);
        DeleteExpenseCommand = new RelayCommand<ExpenseDTO>(
            async expense => await DeleteExpenseAsync(expense),
            expense => expense?.PaymentStatus != SettledStatus);
    }

    private void ShowCreateExpenseForm()
    {
        _editingExpenseId = null;
        ExpenseFormDate = DateTime.Today;
        ExpenseFormDescription = string.Empty;
        ExpenseFormCategory = Categories[0];
        ExpenseFormValue = string.Empty;
        IsCardPaymentMode = false;
        ExpenseFormPaymentSource = Banks.Count > 0 ? Banks[0].Name : string.Empty;
        ExpenseFormCardTag = string.Empty;
        ExpenseFormRoundUpAmount = string.Empty;
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
        ExpenseFormCategory = expense.Category;
        ExpenseFormValue = expense.Value.ToString("0.##");
        IsCardPaymentMode = expense.CardTag != null;
        ExpenseFormPaymentSource = expense.PaymentSource ?? string.Empty;
        ExpenseFormCardTag = expense.CardTag ?? string.Empty;
        ExpenseFormRoundUpAmount = expense.RoundUpAmount?.ToString("0.##") ?? string.Empty;
        ExpenseFormIsSettled = expense.PaymentStatus == SettledStatus;
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
        if (!decimal.TryParse(ExpenseFormValue, out var value))
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
                    PaymentSource = paymentSource,
                    CardTag = cardTag,
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
                    PaymentSource = paymentSource,
                    CardTag = cardTag,
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

    public static readonly IReadOnlyList<string> IncomeSources = ["Gleison", "Ariana", "Lottery", "DividendoJuros"];
    private static readonly HashSet<string> IncomeSourcesWithGrossValue = ["Gleison", "Ariana"];

    private bool _isIncomeFormOpen;
    private Guid? _editingIncomeId;
    private DateTime? _incomeFormDate;
    private string _incomeFormSource = IncomeSources[0];
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
        IncomeFormSource = IncomeSources[0];
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
        IncomeFormSource = income.IncomeSource;
        IncomeFormGrossValue = income.GrossValue?.ToString("0.##") ?? string.Empty;
        IncomeFormNetValue = income.NetValue.ToString("0.##");
        IncomeFormBank = income.Bank;
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
                    IncomeSource = IncomeFormSource,
                    GrossValue = grossValue,
                    NetValue = netValue,
                    Bank = IncomeFormBank,
                });
            }
            else
            {
                await _incomeService.AddIncomeAsync(new IncomeCreateDTO
                {
                    Date = date,
                    IncomeSource = IncomeFormSource,
                    GrossValue = grossValue,
                    NetValue = netValue,
                    Bank = IncomeFormBank,
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

        if (!_confirm($"Delete this income entry from {income.IncomeSource}? This removes it for good."))
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

    // ----- Banks grid -----

    private string? _bankHistoryError;

    public string? BankHistoryError
    {
        get => _bankHistoryError;
        private set => SetProperty(ref _bankHistoryError, value);
    }

    public RelayCommand<BankTotalRow> ToggleBankExpandCommand { get; private set; } = null!;
    public RelayCommand<BankHistoryEntry> DeleteHistoryEntryCommand { get; private set; } = null!;

    private void InitializeBankCommands()
    {
        ToggleBankExpandCommand = new RelayCommand<BankTotalRow>(row =>
        {
            if (row is not null)
            {
                row.IsExpanded = !row.IsExpanded;
            }
        });
        DeleteHistoryEntryCommand = new RelayCommand<BankHistoryEntry>(async entry => await DeleteHistoryEntryAsync(entry));
    }

    private async Task DeleteHistoryEntryAsync(BankHistoryEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        var confirmMessage = entry.Kind == BankHistoryEntryKind.Adjustment
            ? "Delete this balance adjustment? This removes it for good."
            : "Delete this transfer? This removes it for good.";

        if (!_confirm(confirmMessage))
        {
            return;
        }

        BankHistoryError = null;

        try
        {
            if (entry.Transfer is { } transfer)
            {
                await _transferService.DeleteTransferAsync(transfer.Id);
            }
            else if (entry.Adjustment is { } adjustment)
            {
                await _balanceAdjustmentService.DeleteAdjustmentAsync(adjustment.Bank, adjustment.Id);
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            BankHistoryError = ex.Message;
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
        set => SetProperty(ref _transferFormSourceBank, value);
    }

    public string TransferFormDestinationBank
    {
        get => _transferFormDestinationBank;
        set => SetProperty(ref _transferFormDestinationBank, value);
    }

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
        SaveTransferCommand = new RelayCommand(async () => await SaveTransferAsync(), () => !IsSavingTransfer);
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
        TransferFormSourceBank = transfer.SourceBank;
        TransferFormDestinationBank = transfer.DestinationBank;
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
                    Date = date, SourceBank = TransferFormSourceBank, DestinationBank = TransferFormDestinationBank,
                    Amount = amount, Note = note,
                });
            }
            else
            {
                await _transferService.AddTransferAsync(new TransferCreateDTO
                {
                    Date = date, SourceBank = TransferFormSourceBank, DestinationBank = TransferFormDestinationBank,
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
    private string? _editingAdjustmentBank;
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

    public string AdjustmentFormBankName
    {
        get => _adjustmentFormBankName;
        private set => SetProperty(ref _adjustmentFormBankName, value);
    }

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

    public RelayCommand<BankTotalRow> ShowCorrectBalanceFormCommand { get; private set; } = null!;
    public RelayCommand<BankHistoryEntry> EditAdjustmentCommand { get; private set; } = null!;
    public RelayCommand CancelAdjustmentFormCommand { get; private set; } = null!;
    public RelayCommand SaveAdjustmentCommand { get; private set; } = null!;
    public RelayCommand DismissAdjustmentResultCommand { get; private set; } = null!;

    private void InitializeAdjustmentCommands()
    {
        ShowCorrectBalanceFormCommand = new RelayCommand<BankTotalRow>(ShowCreateAdjustmentForm);
        EditAdjustmentCommand = new RelayCommand<BankHistoryEntry>(ShowEditAdjustmentForm);
        CancelAdjustmentFormCommand = new RelayCommand(CloseAdjustmentForm);
        SaveAdjustmentCommand = new RelayCommand(async () => await SaveAdjustmentAsync(), () => !IsSavingAdjustment);
        DismissAdjustmentResultCommand = new RelayCommand(() => AdjustmentSavedDelta = null);
    }

    private void ShowCreateAdjustmentForm(BankTotalRow? row)
    {
        if (row is null)
        {
            return;
        }

        _editingAdjustmentBank = null;
        _editingAdjustmentId = null;
        AdjustmentFormBankName = row.Bank;
        AdjustmentFormCurrentBalance = row.Balance;
        AdjustmentFormDate = DateTime.Today;
        AdjustmentFormTargetBalance = string.Empty;
        AdjustmentFormNote = string.Empty;
        AdjustmentSaveError = null;
        AdjustmentSavedDelta = null;
        OnPropertyChanged(nameof(IsEditingAdjustment));
        IsAdjustmentFormOpen = true;
    }

    private void ShowEditAdjustmentForm(BankHistoryEntry? entry)
    {
        if (entry?.Adjustment is not { } adjustment)
        {
            return;
        }

        var row = BankTotals.FirstOrDefault(b => b.Bank == adjustment.Bank);

        _editingAdjustmentBank = adjustment.Bank;
        _editingAdjustmentId = adjustment.Id;
        AdjustmentFormBankName = adjustment.Bank;
        AdjustmentFormCurrentBalance = row?.Balance ?? 0m;
        AdjustmentFormDate = adjustment.Date.ToDateTime(TimeOnly.MinValue);
        AdjustmentFormTargetBalance = adjustment.TargetBalance.ToString("0.##");
        AdjustmentFormNote = adjustment.Note ?? string.Empty;
        AdjustmentSaveError = null;
        AdjustmentSavedDelta = null;
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
                result = await _balanceAdjustmentService.AddAdjustmentAsync(AdjustmentFormBankName, new BalanceAdjustmentCreateDTO
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
            await _cardStatementService.MarkStatementPaidAsync(statement.Id, new MarkStatementPaidDTO { PaymentSource = paymentSource });
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
