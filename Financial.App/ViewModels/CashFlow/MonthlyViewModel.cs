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

    public ObservableCollection<CategoryTotalDTO> CategoryTotals { get; } = [];
    public ObservableCollection<CategoryTotalDTO> FilteredCategoryTotals { get; } = [];
    public ObservableCollection<BankDTO> Banks { get; } = [];
    public ObservableCollection<BankTotalRow> BankTotals { get; } = [];
    public ObservableCollection<BankTotalRow> FilteredBankTotals { get; } = [];
    public ObservableCollection<IncomeTotalRow> IncomeTotals { get; } = [];
    public ObservableCollection<CreditCardDTO> CreditCards { get; } = [];
    public ObservableCollection<CategoryDTO> Categories { get; } = [];

    public ColumnFilterViewModel<BankTotalRow> BankFilter { get; }
    public ColumnFilterViewModel<CategoryTotalDTO> CategoryFilter { get; }

    public IEnumerable<CategoryDTO> ActiveCategories => Categories.Where(c => c.Active);

    public decimal BankTotalsSum => BankTotals.Sum(b => b.Balance);
    public decimal RoundUpTotalsSum => BankTotals.Sum(b => b.RoundUpTotal);
    public decimal TotalIncoming => IncomeTotals.Sum(i => i.NetValue);

    private TitheSummaryDTO? _titheSummary;
    public TitheSummaryDTO? TitheSummary
    {
        get => _titheSummary;
        private set
        {
            if (SetProperty(ref _titheSummary, value))
            {
                OnPropertyChanged(nameof(HasTitheCarryForward));
                OnPropertyChanged(nameof(CarryForwardAccessibleName));
            }
        }
    }

    public bool HasTitheCarryForward => TitheSummary?.CarryForward != null;

    /// <summary>Fully self-describing accessible name for the carry-forward checkbox - narration
    /// must not depend on the adjacent static "Carry forward" label being read first.</summary>
    public string CarryForwardAccessibleName => TitheSummary?.CarryForward is { } carryForward
        ? $"Carry forward {carryForward.Amount:N2} from {carryForward.FromMonth}/{carryForward.FromYear}"
        : "Carry forward";

    private bool _isUpdatingTitheCarryForward;
    public bool IsUpdatingTitheCarryForward
    {
        get => _isUpdatingTitheCarryForward;
        private set
        {
            if (SetProperty(ref _isUpdatingTitheCarryForward, value))
            {
                OnPropertyChanged(nameof(IsCarryForwardToggleEnabled));
            }
        }
    }

    public bool IsCarryForwardToggleEnabled => !IsUpdatingTitheCarryForward;

    private string? _titheCarryForwardUpdateError;
    public string? TitheCarryForwardUpdateError
    {
        get => _titheCarryForwardUpdateError;
        private set => SetProperty(ref _titheCarryForwardUpdateError, value);
    }

    public decimal CategoryTotalsSum => CategoryTotals.Sum(c => c.TotalValue);

    public RelayCommand RetryCommand { get; }

    public IncomeWorkflowViewModel Income { get; }

    public ExpenseWorkflowViewModel Expense { get; }

    public CardsWorkflowViewModel Cards { get; }

    public BankOperationsWorkflowViewModel BankOperations { get; }

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

        var today = DateTime.Today;
        _year = today.Year;
        _month = today.Month;

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        Income = new IncomeWorkflowViewModel(incomeService, confirm, RefreshAsync);
        Expense = new ExpenseWorkflowViewModel(expenseService, Categories, Banks, CreditCards, confirm, tracer, RefreshAsync);
        Cards = new CardsWorkflowViewModel(cardStatementService, creditCardService, Banks, CreditCards, RefreshAsync);
        BankOperations = new BankOperationsWorkflowViewModel(transferService, balanceAdjustmentService, Banks, BankTotals, confirm, RefreshAsync);

        BankFilter = new ColumnFilterViewModel<BankTotalRow>("Bank", row => [row.Bank], ApplyBankFilter);
        CategoryFilter = new ColumnFilterViewModel<CategoryTotalDTO>("Category", c => [c.Category], ApplyCategoryFilter);
    }

    /// <summary>Toggles whether the previous month's carry-forward counts toward this month's Tithe
    /// Balance. Echo-guarded: WPF re-raises Checked/Unchecked when the bound data is rebuilt
    /// (RefreshAsync) even without a real user edit, matching CardsWorkflowViewModel's pattern.</summary>
    internal async Task UpdateTitheCarryForwardAsync(bool included)
    {
        if (TitheSummary?.CarryForward is not { } carryForward || carryForward.Included == included)
        {
            return;
        }

        TitheCarryForwardUpdateError = null;
        IsUpdatingTitheCarryForward = true;

        try
        {
            await _titheService.UpdateCarryForwardInclusionAsync(Year, Month, included);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            TitheCarryForwardUpdateError = ex.Message;
        }
        finally
        {
            IsUpdatingTitheCarryForward = false;
        }
    }

    private void ApplyBankFilter() => ReplaceAll(FilteredBankTotals, BankTotals.Where(BankFilter.Matches));

    private void ApplyCategoryFilter() => ReplaceAll(FilteredCategoryTotals, CategoryTotals.Where(CategoryFilter.Matches));

    private int _refreshRequestId;

    /// <summary>
    /// Reloads expenses/incomes/category totals/banks/tithe for the selected period. The caller
    /// is responsible for triggering the initial load (e.g. the hosting view's Loaded event).
    /// Guards against overlapping calls (e.g. a rapid year/month change racing a manual retry)
    /// by discarding a completion whose request has been superseded.
    /// </summary>
    internal Task RefreshAsync() => ExecuteRefreshAsync(
        () => ++_refreshRequestId,
        id => id == _refreshRequestId,
        loading => IsLoading = loading,
        error => Error = error,
        async isCurrent =>
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
            var titheSummaryTask = _titheService.GetTitheSummaryAsync(year, month);
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

            if (!isCurrent())
            {
                return;
            }

            Expense.ApplyRefresh(expenses, unpaidCardCharges);
            ReplaceAll(CategoryTotals, categoryTotals);
            ReplaceAll(Banks, banks);
            Income.ApplyRefresh(incomes, incomeSources, banks);
            TitheSummary = titheSummary;
            OnPropertyChanged(nameof(CategoryTotalsSum));
            CategoryFilter.Refresh(CategoryTotals);
            ApplyCategoryFilter();

            var newBankTotals = BuildBankTotals(banks, expenses, bankBalances);
            ReplaceAll(BankTotals, newBankTotals);
            OnPropertyChanged(nameof(BankTotalsSum));
            OnPropertyChanged(nameof(RoundUpTotalsSum));
            BankFilter.Refresh(BankTotals);
            ApplyBankFilter();

            BankOperations.ApplyRefresh(transfers, adjustmentsByBank, year, month, banks);

            Cards.ApplyRefresh(cardStatements);

            ReplaceAll(CreditCards, creditCards);
            Expense.NotifyCreditCardsChanged();
            Cards.NotifyCreditCardsChanged();

            ReplaceAll(Categories, categories);
            OnPropertyChanged(nameof(ActiveCategories));
            Expense.NotifyCategoriesChanged();

            ReplaceAll(IncomeTotals, BuildIncomeTotals(incomes));
            OnPropertyChanged(nameof(TotalIncoming));
        });

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
}
