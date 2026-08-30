using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class IncomeSplitViewModel : ViewModelBase
{
    private readonly IReserveService _reserveService;
    private readonly Action _closeOtherForms;
    private readonly Func<Task> _refresh;

    private bool _isSplitFormOpen;
    private DateTime? _splitDate;
    private string _splitAmount = string.Empty;
    private string _splitDescription = string.Empty;
    private bool _isSubmittingSplit;
    private string? _splitSaveError;
    private IncomeSplitResultDTO? _lastSplitResult;

    // Persistent create-form default (P38-F10) - read on the next ShowSplitForm, written back
    // after every successful submit.
    private DateTime? _lastUsedSplitDate;

    public bool IsSplitFormOpen
    {
        get => _isSplitFormOpen;
        private set
        {
            if (SetProperty(ref _isSplitFormOpen, value))
            {
                OnPropertyChanged(nameof(ShowSplitFormFields));
            }
        }
    }

    public DateTime? SplitDate
    {
        get => _splitDate;
        set => SetProperty(ref _splitDate, value);
    }

    public string SplitAmount
    {
        get => _splitAmount;
        set => SetProperty(ref _splitAmount, value);
    }

    public string SplitDescription
    {
        get => _splitDescription;
        set => SetProperty(ref _splitDescription, value);
    }

    public bool IsSubmittingSplit
    {
        get => _isSubmittingSplit;
        private set => SetProperty(ref _isSubmittingSplit, value);
    }

    public string? SplitSaveError
    {
        get => _splitSaveError;
        private set
        {
            if (SetProperty(ref _splitSaveError, value))
            {
                OnPropertyChanged(nameof(DateFieldError));
                OnPropertyChanged(nameof(AmountFieldError));
                OnPropertyChanged(nameof(DescriptionFieldError));
            }
        }
    }

    /// <summary>
    /// Per-field validation errors (P38-F05) — same substring-match pattern as F02/F04's
    /// derived field-error properties, matching this form's own client-side
    /// <see cref="IncomeSplitFormValidation"/> text.
    /// </summary>
    public string? DateFieldError => MatchFieldError("Date is required.");

    public string? AmountFieldError => MatchFieldError("Amount must be a positive number.");

    public string? DescriptionFieldError => MatchFieldError("Description is required.");

    private string? MatchFieldError(string fragment) =>
        SplitSaveError is { } error && error.Contains(fragment, StringComparison.OrdinalIgnoreCase)
            ? error
            : null;

    public IncomeSplitResultDTO? LastSplitResult
    {
        get => _lastSplitResult;
        private set
        {
            if (SetProperty(ref _lastSplitResult, value))
            {
                OnPropertyChanged(nameof(HasSplitResult));
                OnPropertyChanged(nameof(ShowSplitFormFields));
            }
        }
    }

    public bool HasSplitResult => LastSplitResult != null;

    public bool ShowSplitFormFields => IsSplitFormOpen && LastSplitResult == null;

    public RelayCommand ShowSplitFormCommand { get; }
    public RelayCommand CancelSplitFormCommand { get; }
    public RelayCommand SubmitSplitCommand { get; }
    public RelayCommand DismissSplitResultCommand { get; }

    public IncomeSplitViewModel(IReserveService reserveService, Action closeOtherForms, Func<Task> refresh)
    {
        _reserveService = reserveService ?? throw new ArgumentNullException(nameof(reserveService));
        _closeOtherForms = closeOtherForms ?? throw new ArgumentNullException(nameof(closeOtherForms));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        ShowSplitFormCommand = new RelayCommand(ShowSplitForm);
        CancelSplitFormCommand = new RelayCommand(CloseSplitForm);
        SubmitSplitCommand = new RelayCommand(async () => await SubmitSplitAsync());
        DismissSplitResultCommand = new RelayCommand(CloseSplitForm);
    }

    internal void ShowSplitForm()
    {
        _closeOtherForms();
        SplitDate = _lastUsedSplitDate ?? DateTime.Today;
        SplitAmount = string.Empty;
        SplitDescription = string.Empty;
        SplitSaveError = null;
        LastSplitResult = null;
        IsSplitFormOpen = true;
    }

    internal void CloseSplitForm()
    {
        IsSplitFormOpen = false;
        SplitSaveError = null;
        LastSplitResult = null;
    }

    internal Task SubmitSplitAsync() => ExecuteSaveAsync(
        () => IncomeSplitFormValidation.BuildValidationMessage(SplitDate, SplitAmount, SplitDescription),
        error => SplitSaveError = error,
        saving => IsSubmittingSplit = saving,
        async () =>
        {
            var result = await _reserveService.PostIncomeSplitAsync(new IncomeSplitRequestDTO
            {
                Date = DateOnly.FromDateTime(SplitDate!.Value),
                Amount = decimal.Parse(SplitAmount),
                Description = SplitDescription,
            });

            _lastUsedSplitDate = SplitDate;

            LastSplitResult = result;
            await _refresh();
        });
}
