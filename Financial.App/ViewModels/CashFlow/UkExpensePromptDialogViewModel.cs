using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public enum UkExpensePromptDecision
{
    Confirm,
    Skip,
    Cancel,
}

/// <summary>
/// Collects the fields for the standalone Expense a UK Mensais bill can generate when marked Paid.
/// Deliberately "dumb": it only gathers form input and reports which of Confirm/Skip/Cancel the
/// user chose. MensaisViewModel owns the actual Expense-creation and status-commit orchestration,
/// mirroring how BanksViewModel calls IBankService itself after ShowBankFormDialog returns rather
/// than having the dialog VM call services directly.
/// </summary>
public sealed class UkExpensePromptDialogViewModel : ViewModelBase
{
    private string _description;
    private string _value;
    private DateTime _date;
    private Guid? _bankId;
    private Guid? _categoryId;
    private string _validationMessage = string.Empty;

    public string BillDescription { get; }

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                Validate();
            }
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                Validate();
            }
        }
    }

    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public Guid? BankId
    {
        get => _bankId;
        set
        {
            if (SetProperty(ref _bankId, value))
            {
                Validate();
            }
        }
    }

    public Guid? CategoryId
    {
        get => _categoryId;
        set
        {
            if (SetProperty(ref _categoryId, value))
            {
                Validate();
            }
        }
    }

    public IReadOnlyList<BankDTO> Banks { get; }

    public IReadOnlyList<CategoryDTO> Categories { get; }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public UkExpensePromptDecision Decision { get; private set; }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand SkipCommand { get; }

    public RelayCommand CancelCommand { get; }

    public event EventHandler<bool?>? CloseRequested;

    public UkExpensePromptDialogViewModel(RecurringBillDTO bill, IReadOnlyList<BankDTO> banks, IReadOnlyList<CategoryDTO> categories)
    {
        ArgumentNullException.ThrowIfNull(bill);
        Banks = banks ?? throw new ArgumentNullException(nameof(banks));
        Categories = categories ?? throw new ArgumentNullException(nameof(categories));

        BillDescription = bill.Description;
        _description = bill.Description;
        _value = bill.Value.ToString();
        _date = DateTime.Today;

        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        SkipCommand = new RelayCommand(Skip);
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

        Decision = UkExpensePromptDecision.Confirm;
        CloseRequested?.Invoke(this, true);
    }

    private void Skip()
    {
        Decision = UkExpensePromptDecision.Skip;
        CloseRequested?.Invoke(this, true);
    }

    private void Cancel()
    {
        Decision = UkExpensePromptDecision.Cancel;
        CloseRequested?.Invoke(this, false);
    }

    private bool CanConfirm() => string.IsNullOrWhiteSpace(ValidationMessage);

    private void Validate()
    {
        ValidationMessage = string.IsNullOrWhiteSpace(Description)
            ? "Description is required."
            : !decimal.TryParse(Value, out var parsedValue) || parsedValue <= 0
                ? "Value must be a number greater than zero."
                : BankId is null
                    ? "Bank is required."
                    : CategoryId is null
                        ? "Category is required."
                        : string.Empty;
        ConfirmCommand.RaiseCanExecuteChanged();
    }
}
