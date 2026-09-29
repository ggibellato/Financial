using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class WithdrawalViewModel : ViewModelBase
{
    private readonly IReserveService _reserveService;
    private readonly Func<string, bool> _confirm;
    private readonly Action _closeOtherForms;
    private readonly Func<Task> _refresh;

    private bool _isWithdrawalFormOpen;
    private Guid? _withdrawalBucketId;
    private string _withdrawalAmount = string.Empty;
    private DateTime? _withdrawalDate;
    private string _withdrawalDescription = string.Empty;
    private bool _isSubmittingWithdrawal;
    private string? _withdrawalSaveError;

    // Persistent create-form defaults (P38-F10) - read on the next ShowWithdrawalForm, written
    // back after every successful submit.
    private DateTime? _lastUsedWithdrawalDate;
    private Guid? _lastUsedWithdrawalBucketId;

    private WithdrawalBankOption _selectedBankOption = WithdrawalBankOption.Direct;
    private Guid? _explicitExpenseCategoryId;

    /// <summary>The same instance ReservaViewModel owns — mutated in place by its refresh, never replaced.</summary>
    public ObservableCollection<ReserveBucketDTO> Buckets { get; }

    public ObservableCollection<WithdrawalBankOption> BankOptions { get; } = [WithdrawalBankOption.Direct];

    public ObservableCollection<CategoryDTO> CategoryOptions { get; } = [];

    public bool IsWithdrawalFormOpen
    {
        get => _isWithdrawalFormOpen;
        private set => SetProperty(ref _isWithdrawalFormOpen, value);
    }

    public Guid? WithdrawalBucketId
    {
        get => _withdrawalBucketId;
        set
        {
            if (SetProperty(ref _withdrawalBucketId, value))
            {
                OnPropertyChanged(nameof(WithdrawalExpenseCategoryId));
            }
        }
    }

    public WithdrawalBankOption SelectedBankOption
    {
        get => _selectedBankOption;
        set
        {
            var option = value ?? WithdrawalBankOption.Direct;
            if (!SetProperty(ref _selectedBankOption, option))
            {
                return;
            }

            if (option.Id is null)
            {
                _explicitExpenseCategoryId = null;
            }

            OnPropertyChanged(nameof(IsBankSelected));
            OnPropertyChanged(nameof(WithdrawalExpenseCategoryId));
        }
    }

    public bool IsBankSelected => SelectedBankOption.Id is not null;

    public Guid? WithdrawalExpenseCategoryId
    {
        get => _explicitExpenseCategoryId ?? DefaultExpenseCategoryId();
        set => SetProperty(ref _explicitExpenseCategoryId, value);
    }

    public string WithdrawalAmount
    {
        get => _withdrawalAmount;
        set => SetProperty(ref _withdrawalAmount, value);
    }

    public DateTime? WithdrawalDate
    {
        get => _withdrawalDate;
        set => SetProperty(ref _withdrawalDate, value);
    }

    public string WithdrawalDescription
    {
        get => _withdrawalDescription;
        set => SetProperty(ref _withdrawalDescription, value);
    }

    public bool IsSubmittingWithdrawal
    {
        get => _isSubmittingWithdrawal;
        private set
        {
            if (SetProperty(ref _isSubmittingWithdrawal, value))
            {
                OnPropertyChanged(nameof(IsWithdrawalFormEditable));
            }
        }
    }

    public bool IsWithdrawalFormEditable => !IsSubmittingWithdrawal;

    public string? WithdrawalSaveError
    {
        get => _withdrawalSaveError;
        private set
        {
            if (SetProperty(ref _withdrawalSaveError, value))
            {
                OnPropertyChanged(nameof(DateFieldError));
                OnPropertyChanged(nameof(BucketFieldError));
                OnPropertyChanged(nameof(DescriptionFieldError));
                OnPropertyChanged(nameof(AmountFieldError));
                OnPropertyChanged(nameof(ExpenseCategoryFieldError));
                OnPropertyChanged(nameof(WithdrawalGeneralSaveError));
            }
        }
    }

    /// <summary>
    /// Per-field validation errors (P38-F05) — same substring-match pattern as F02/F04's
    /// derived field-error properties, matching this form's own client-side
    /// <see cref="WithdrawalFormValidation"/> text.
    /// </summary>
    public string? DateFieldError => MatchFieldError("Date is required.");

    public string? BucketFieldError => MatchFieldError("Bucket is required.");

    public string? DescriptionFieldError => MatchFieldError("Description is required.");

    public string? AmountFieldError => MatchFieldError("Amount must be a positive number.");

    public string? ExpenseCategoryFieldError => MatchFieldError(WithdrawalFormValidation.ExpenseCategoryRequired);

    /// <summary>Bottom-of-form message — shown only when the error isn't already attributed to a field above.</summary>
    public string? WithdrawalGeneralSaveError =>
        DateFieldError is null && BucketFieldError is null &&
        DescriptionFieldError is null && AmountFieldError is null && ExpenseCategoryFieldError is null
            ? WithdrawalSaveError
            : null;

    private string? MatchFieldError(string fragment) =>
        WithdrawalSaveError?.Split(Environment.NewLine)
            .FirstOrDefault(line => line.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    public RelayCommand ShowWithdrawalFormCommand { get; }
    public RelayCommand CancelWithdrawalFormCommand { get; }
    public RelayCommand SubmitWithdrawalCommand { get; }

    public WithdrawalViewModel(
        IReserveService reserveService, ObservableCollection<ReserveBucketDTO> buckets,
        Func<string, bool> confirm, Action closeOtherForms, Func<Task> refresh)
    {
        _reserveService = reserveService ?? throw new ArgumentNullException(nameof(reserveService));
        Buckets = buckets ?? throw new ArgumentNullException(nameof(buckets));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _closeOtherForms = closeOtherForms ?? throw new ArgumentNullException(nameof(closeOtherForms));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        ShowWithdrawalFormCommand = new RelayCommand(ShowWithdrawalForm);
        CancelWithdrawalFormCommand = new RelayCommand(CloseWithdrawalForm);
        SubmitWithdrawalCommand = new RelayCommand(async () => await SubmitWithdrawalAsync());
    }

    internal Guid? DefaultBucketId() =>
        (Buckets.Where(b => b.IsActive).FirstOrDefault() ?? Buckets.FirstOrDefault())?.Id;

    internal void LoadReferenceData(IEnumerable<BankDTO> banks, IEnumerable<CategoryDTO> categories)
    {
        while (BankOptions.Count > 1)
        {
            BankOptions.RemoveAt(BankOptions.Count - 1);
        }

        foreach (var bank in banks)
        {
            BankOptions.Add(new WithdrawalBankOption(bank.Id, bank.Name));
        }

        ReplaceAll(CategoryOptions, WithdrawalCategoryRules.Eligible(categories));
        OnPropertyChanged(nameof(WithdrawalExpenseCategoryId));
    }

    private Guid? DefaultExpenseCategoryId() =>
        WithdrawalCategoryRules.DefaultFor(CategoryOptions, Buckets.FirstOrDefault(b => b.Id == WithdrawalBucketId)?.Name);

    internal void ShowWithdrawalForm()
    {
        _closeOtherForms();
        WithdrawalBucketId = _lastUsedWithdrawalBucketId is { } lastBucket && Buckets.Any(b => b.Id == lastBucket)
            ? lastBucket
            : DefaultBucketId();
        WithdrawalAmount = string.Empty;
        WithdrawalDate = _lastUsedWithdrawalDate ?? DateTime.Today;
        WithdrawalDescription = string.Empty;
        _explicitExpenseCategoryId = null;
        SelectedBankOption = WithdrawalBankOption.Direct;
        OnPropertyChanged(nameof(WithdrawalExpenseCategoryId));
        WithdrawalSaveError = null;
        IsWithdrawalFormOpen = true;
    }

    internal void CloseWithdrawalForm()
    {
        IsWithdrawalFormOpen = false;
        WithdrawalSaveError = null;
    }

    internal Task SubmitWithdrawalAsync() => ExecuteSaveAsync(
        () => WithdrawalFormValidation.BuildValidationMessage(
            WithdrawalBucketId, WithdrawalAmount, WithdrawalDate, WithdrawalDescription,
            SelectedBankOption.Id, WithdrawalExpenseCategoryId),
        error => WithdrawalSaveError = error,
        saving => IsSubmittingWithdrawal = saving,
        async () =>
        {
            await PostWithdrawalWithOverdraftHandlingAsync(confirmed: false);

            _lastUsedWithdrawalDate = WithdrawalDate;
            _lastUsedWithdrawalBucketId = WithdrawalBucketId;

            CloseWithdrawalForm();
            await _refresh();
        });

    /// <summary>
    /// Posts the withdrawal; on an overdraft conflict, asks the user to confirm and resubmits
    /// with the override flag set. Declining re-throws the server's conflict message so the
    /// caller's catch block surfaces it as WithdrawalSaveError. Mirrors useReserva.ts's
    /// ApiError(409) + window.confirm flow.
    /// </summary>
    private async Task PostWithdrawalWithOverdraftHandlingAsync(bool confirmed)
    {
        var request = new WithdrawalRequestDTO
        {
            BucketId = WithdrawalBucketId!.Value,
            Amount = decimal.Parse(WithdrawalAmount),
            Date = DateOnly.FromDateTime(WithdrawalDate!.Value),
            Description = WithdrawalDescription,
            Confirmed = confirmed,
            PaymentSourceBankId = SelectedBankOption.Id,
            ExpenseCategoryId = SelectedBankOption.Id is null ? null : WithdrawalExpenseCategoryId,
        };

        try
        {
            await _reserveService.PostWithdrawalAsync(request);
        }
        catch (OverdraftConfirmationRequiredException ex) when (!confirmed)
        {
            if (!_confirm($"{ex.Message}\n\nProceed anyway?"))
            {
                throw;
            }

            await PostWithdrawalWithOverdraftHandlingAsync(confirmed: true);
        }
    }
}
