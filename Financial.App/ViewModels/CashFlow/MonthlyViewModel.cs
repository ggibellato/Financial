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

    private TitheSummaryDTO? _titheSummary;
    public TitheSummaryDTO? TitheSummary
    {
        get => _titheSummary;
        private set => SetProperty(ref _titheSummary, value);
    }

    public decimal CategoryTotalsSum => CategoryTotals.Sum(c => c.TotalValue);

    public RelayCommand RetryCommand { get; }

    public MonthlyViewModel(
        IExpenseService expenseService,
        IIncomeService incomeService,
        IBankService bankService,
        ITitheService titheService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _titheService = titheService ?? throw new ArgumentNullException(nameof(titheService));

        var today = DateTime.Today;
        _year = today.Year;
        _month = today.Month;

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        InitializeExpenseCommands();

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
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
            var titheSummary = await Task.Run(() => _titheService.GetTitheSummary(year, month));

            ReplaceAll(Expenses, expenses);
            ReplaceAll(Incomes, incomes);
            ReplaceAll(CategoryTotals, categoryTotals);
            ReplaceAll(Banks, banks);
            TitheSummary = titheSummary;
            OnPropertyChanged(nameof(CategoryTotalsSum));
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsLoading = false;
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

    private async Task SaveExpenseAsync()
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

    private async Task DeleteExpenseAsync(ExpenseDTO? expense)
    {
        if (expense is null)
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
}
