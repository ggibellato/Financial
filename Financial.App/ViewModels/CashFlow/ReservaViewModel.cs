using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// ViewModel for the Reserva tab: bucket balances, movement history (with split-group
/// subtotals), Income Split, Withdrawal (with overdraft confirmation), and movement
/// edit/delete. Mirrors Financial.Web's useReserva.ts hook. Standalone from
/// <see cref="MonthlyViewModel"/> — Reserva is its own top-level destination with no shared
/// state with Monthly.
/// </summary>
public class ReservaViewModel : ViewModelBase
{
    private const decimal SplitPercentageTolerance = 0.01m;

    private readonly IReserveService _reserveService;
    private readonly IReserveBucketService _reserveBucketService;
    private readonly Func<string, bool> _confirm;
    private readonly ILogger<ReservaViewModel> _logger;

    private bool _isLoading = true;
    private string? _error;

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

    public ObservableCollection<ReserveBucketBalanceDTO> Balances { get; } = [];
    public ObservableCollection<ReserveMovementRow> Movements { get; } = [];
    public ObservableCollection<ReserveBucketDTO> Buckets { get; } = [];

    public decimal TotalBalance => Balances.Sum(b => b.Balance);

    private IEnumerable<ReserveBucketDTO> ActiveBuckets => Buckets.Where(b => b.IsActive);

    /// <summary>Empty when active buckets' percentages sum within 99.99-100.01, a warning message otherwise.</summary>
    public string SplitPercentageWarning
    {
        get
        {
            if (Buckets.Count == 0)
            {
                return string.Empty;
            }

            var activeSum = ActiveBuckets.Sum(b => b.SplitPercentage);
            if (Math.Abs(activeSum - 100m) <= SplitPercentageTolerance)
            {
                return string.Empty;
            }

            return $"Active bucket percentages sum to {activeSum:N2}%, not 100%";
        }
    }

    /// <summary>First active bucket's name, falling back to the first bucket overall, or empty when none loaded.</summary>
    private string DefaultBucketName() =>
        (ActiveBuckets.FirstOrDefault() ?? Buckets.FirstOrDefault())?.Name ?? string.Empty;

    public RelayCommand RetryCommand { get; }

    public ReservaViewModel(IReserveService reserveService, IReserveBucketService reserveBucketService, Func<string, bool> confirm, ILogger<ReservaViewModel> logger)
    {
        _reserveService = reserveService ?? throw new ArgumentNullException(nameof(reserveService));
        _reserveBucketService = reserveBucketService ?? throw new ArgumentNullException(nameof(reserveBucketService));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        InitializeSplitCommands();
        InitializeWithdrawalCommands();
        InitializeEditDeleteCommands();

        _ = RefreshAsync();
    }

    private int _refreshRequestId;

    /// <summary>
    /// Reloads balances and movement history. Guards against overlapping calls (e.g. the
    /// constructor's initial load racing a manual retry) by discarding a completion whose
    /// request has been superseded.
    /// </summary>
    /// <param name="includeBuckets">
    /// Whether to also reload <see cref="Buckets"/>. Rebuilding that collection clears it before
    /// re-adding items, which resets a bound ComboBox's SelectedValue — harmless on initial
    /// load/retry (no bucket form can be open yet), but a mutation-triggered refresh can run
    /// while the Withdrawal or Edit form is still open (their form panel isn't mutually exclusive
    /// with the movement grid's row actions), so those callers pass false to avoid silently
    /// clearing the user's in-progress bucket selection. Buckets are seeded-only and never change
    /// mid-session, so skipping the reload there loses nothing.
    /// </param>
    internal async Task RefreshAsync(bool includeBuckets = true)
    {
        var requestId = ++_refreshRequestId;
        IsLoading = true;
        Error = null;

        try
        {
            var balancesTask = Task.Run(() => _reserveService.GetBucketBalances());
            var movementsTask = Task.Run(() => _reserveService.GetMovementHistory());
            var bucketsTask = includeBuckets ? Task.Run(TryGetReserveBuckets) : null;
            var pendingTasks = new List<Task> { balancesTask, movementsTask };
            if (bucketsTask is not null)
            {
                pendingTasks.Add(bucketsTask);
            }
            await Task.WhenAll(pendingTasks);
            var balances = balancesTask.Result;
            var movements = movementsTask.Result;

            if (requestId != _refreshRequestId)
            {
                return;
            }

            ReplaceAll(Balances, balances);
            OnPropertyChanged(nameof(TotalBalance));

            ReplaceAll(Movements, ReserveMovementRow.BuildRows(movements));

            if (bucketsTask is not null)
            {
                ReplaceAll(Buckets, bucketsTask.Result);
                OnPropertyChanged(nameof(SplitPercentageWarning));
                if (string.IsNullOrEmpty(WithdrawalBucket))
                {
                    WithdrawalBucket = DefaultBucketName();
                }
            }
        }
        catch (Exception ex)
        {
            // error.type only - the message may embed bucket names/balances (FR-014).
            _logger.LogError("Reserva refresh failed with {ErrorType}", ex.GetType().Name);
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

    /// <summary>Bucket metadata is optional display data: a failure here degrades to an empty list instead of failing the whole refresh.</summary>
    private IReadOnlyList<ReserveBucketDTO> TryGetReserveBuckets()
    {
        try
        {
            return _reserveBucketService.GetReserveBuckets();
        }
        catch (Exception ex)
        {
            // Optional display data - degrade to an empty list, but visibly in the log stream.
            _logger.LogWarning("Reserve buckets lookup failed with {ErrorType}; continuing with an empty list", ex.GetType().Name);
            return [];
        }
    }

    /// <summary>Closes all inline forms — only one form panel may be open at a time.</summary>
    private void CloseAllForms()
    {
        CloseSplitForm();
        CloseWithdrawalForm();
        CloseEditForm();
    }

    #region Income Split

    private bool _isSplitFormOpen;
    private DateTime? _splitDate;
    private string _splitAmount = string.Empty;
    private string _splitDescription = string.Empty;
    private bool _isSubmittingSplit;
    private string? _splitSaveError;
    private IncomeSplitResultDTO? _lastSplitResult;

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
        private set => SetProperty(ref _splitSaveError, value);
    }

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

    /// <summary>True while the panel is open and showing the entry fields (not the post-save result).</summary>
    public bool ShowSplitFormFields => IsSplitFormOpen && LastSplitResult == null;

    public RelayCommand ShowSplitFormCommand { get; private set; } = null!;
    public RelayCommand CancelSplitFormCommand { get; private set; } = null!;
    public RelayCommand SubmitSplitCommand { get; private set; } = null!;
    public RelayCommand DismissSplitResultCommand { get; private set; } = null!;

    private void InitializeSplitCommands()
    {
        ShowSplitFormCommand = new RelayCommand(ShowSplitForm);
        CancelSplitFormCommand = new RelayCommand(CloseSplitForm);
        SubmitSplitCommand = new RelayCommand(async () => await SubmitSplitAsync());
        DismissSplitResultCommand = new RelayCommand(CloseSplitForm);
    }

    private void ShowSplitForm()
    {
        CloseAllForms();
        SplitDate = DateTime.Today;
        SplitAmount = string.Empty;
        SplitDescription = string.Empty;
        SplitSaveError = null;
        LastSplitResult = null;
        IsSplitFormOpen = true;
    }

    private void CloseSplitForm()
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

            LastSplitResult = result;
            await RefreshAsync(includeBuckets: false);
        });

    #endregion

    #region Withdrawal

    private bool _isWithdrawalFormOpen;
    private string _withdrawalBucket = string.Empty;
    private string _withdrawalAmount = string.Empty;
    private DateTime? _withdrawalDate;
    private string _withdrawalDescription = string.Empty;
    private bool _isSubmittingWithdrawal;
    private string? _withdrawalSaveError;

    public bool IsWithdrawalFormOpen
    {
        get => _isWithdrawalFormOpen;
        private set => SetProperty(ref _isWithdrawalFormOpen, value);
    }

    public string WithdrawalBucket
    {
        get => _withdrawalBucket;
        set => SetProperty(ref _withdrawalBucket, value);
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
        private set => SetProperty(ref _isSubmittingWithdrawal, value);
    }

    public string? WithdrawalSaveError
    {
        get => _withdrawalSaveError;
        private set => SetProperty(ref _withdrawalSaveError, value);
    }

    public RelayCommand ShowWithdrawalFormCommand { get; private set; } = null!;
    public RelayCommand CancelWithdrawalFormCommand { get; private set; } = null!;
    public RelayCommand SubmitWithdrawalCommand { get; private set; } = null!;

    private void InitializeWithdrawalCommands()
    {
        ShowWithdrawalFormCommand = new RelayCommand(ShowWithdrawalForm);
        CancelWithdrawalFormCommand = new RelayCommand(CloseWithdrawalForm);
        SubmitWithdrawalCommand = new RelayCommand(async () => await SubmitWithdrawalAsync());
    }

    private void ShowWithdrawalForm()
    {
        CloseAllForms();
        WithdrawalBucket = DefaultBucketName();
        WithdrawalAmount = string.Empty;
        WithdrawalDate = DateTime.Today;
        WithdrawalDescription = string.Empty;
        WithdrawalSaveError = null;
        IsWithdrawalFormOpen = true;
    }

    private void CloseWithdrawalForm()
    {
        IsWithdrawalFormOpen = false;
        WithdrawalSaveError = null;
    }

    internal Task SubmitWithdrawalAsync() => ExecuteSaveAsync(
        () => WithdrawalFormValidation.BuildValidationMessage(WithdrawalBucket, WithdrawalAmount, WithdrawalDate, WithdrawalDescription),
        error => WithdrawalSaveError = error,
        saving => IsSubmittingWithdrawal = saving,
        async () =>
        {
            await PostWithdrawalWithOverdraftHandlingAsync(confirmed: false);
            CloseWithdrawalForm();
            await RefreshAsync(includeBuckets: false);
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
            Bucket = WithdrawalBucket,
            Amount = decimal.Parse(WithdrawalAmount),
            Date = DateOnly.FromDateTime(WithdrawalDate!.Value),
            Description = WithdrawalDescription,
            Confirmed = confirmed,
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

    #endregion

    #region Edit and Delete Movement

    private bool _isEditFormOpen;
    private Guid? _editingMovementId;
    private string _editBucket = string.Empty;
    private string _editAmount = string.Empty;
    private DateTime? _editDate;
    private string _editDescription = string.Empty;
    private bool _isSavingMovement;
    private string? _editSaveError;

    public bool IsEditFormOpen
    {
        get => _isEditFormOpen;
        private set => SetProperty(ref _isEditFormOpen, value);
    }

    public string EditBucket
    {
        get => _editBucket;
        set => SetProperty(ref _editBucket, value);
    }

    public string EditAmount
    {
        get => _editAmount;
        set => SetProperty(ref _editAmount, value);
    }

    public DateTime? EditDate
    {
        get => _editDate;
        set => SetProperty(ref _editDate, value);
    }

    public string EditDescription
    {
        get => _editDescription;
        set => SetProperty(ref _editDescription, value);
    }

    public bool IsSavingMovement
    {
        get => _isSavingMovement;
        private set => SetProperty(ref _isSavingMovement, value);
    }

    public string? EditSaveError
    {
        get => _editSaveError;
        private set => SetProperty(ref _editSaveError, value);
    }

    private string? _deleteMovementError;

    public string? DeleteMovementError
    {
        get => _deleteMovementError;
        private set => SetProperty(ref _deleteMovementError, value);
    }

    public RelayCommand<ReserveMovementRow> EditMovementCommand { get; private set; } = null!;
    public RelayCommand CancelEditFormCommand { get; private set; } = null!;
    public RelayCommand SaveMovementEditCommand { get; private set; } = null!;
    public RelayCommand<ReserveMovementRow> DeleteMovementCommand { get; private set; } = null!;

    private void InitializeEditDeleteCommands()
    {
        EditMovementCommand = new RelayCommand<ReserveMovementRow>(ShowEditForm);
        CancelEditFormCommand = new RelayCommand(CloseEditForm);
        SaveMovementEditCommand = new RelayCommand(async () => await SaveMovementEditAsync());
        DeleteMovementCommand = new RelayCommand<ReserveMovementRow>(async row => await DeleteMovementAsync(row));
    }

    private void ShowEditForm(ReserveMovementRow? row)
    {
        if (row is null)
        {
            return;
        }

        CloseAllForms();
        _editingMovementId = row.Id;
        EditBucket = row.Bucket;
        EditAmount = row.Amount.ToString();
        EditDate = row.Date.ToDateTime(TimeOnly.MinValue);
        EditDescription = row.Description;
        EditSaveError = null;
        IsEditFormOpen = true;
    }

    private void CloseEditForm()
    {
        IsEditFormOpen = false;
        EditSaveError = null;
        _editingMovementId = null;
    }

    internal Task SaveMovementEditAsync()
    {
        if (_editingMovementId is not { } id)
        {
            return Task.CompletedTask;
        }

        return ExecuteSaveAsync(
            () => EditReserveMovementFormValidation.BuildValidationMessage(EditBucket, EditAmount, EditDate, EditDescription),
            error => EditSaveError = error,
            saving => IsSavingMovement = saving,
            async () =>
            {
                await _reserveService.UpdateMovementAsync(id, new UpdateReserveMovementDTO
                {
                    Bucket = EditBucket,
                    Amount = decimal.Parse(EditAmount),
                    Date = DateOnly.FromDateTime(EditDate!.Value),
                    Description = EditDescription,
                });

                CloseEditForm();
                await RefreshAsync(includeBuckets: false);
            });
    }

    internal async Task DeleteMovementAsync(ReserveMovementRow? row)
    {
        if (row is null)
        {
            return;
        }

        var confirmMessage = row.IsPartOfGroup
            ? $"Delete \"{row.Description}\"? This is part of a split and will delete all 4 lines."
            : $"Delete \"{row.Description}\"? This removes it for good.";

        if (!_confirm(confirmMessage))
        {
            return;
        }

        DeleteMovementError = null;

        try
        {
            await _reserveService.DeleteMovementAsync(row.Id);
            await RefreshAsync(includeBuckets: false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Reserva delete movement failed with {ErrorType}", ex.GetType().Name);
            DeleteMovementError = ex.Message;
        }
    }

    #endregion
}
