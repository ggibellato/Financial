using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

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
    public static readonly string[] Buckets = ["Investimento", "HouseTreats", "Ariana", "Gleison"];

    private readonly IReserveService _reserveService;
    private readonly Func<string, bool> _confirm;

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

    public decimal TotalBalance => Balances.Sum(b => b.Balance);

    public RelayCommand RetryCommand { get; }

    public ReservaViewModel(IReserveService reserveService, Func<string, bool> confirm)
    {
        _reserveService = reserveService ?? throw new ArgumentNullException(nameof(reserveService));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        InitializeSplitCommands();

        _ = RefreshAsync();
    }

    private int _refreshRequestId;

    /// <summary>
    /// Reloads balances and movement history. Guards against overlapping calls (e.g. the
    /// constructor's initial load racing a manual retry) by discarding a completion whose
    /// request has been superseded.
    /// </summary>
    internal async Task RefreshAsync()
    {
        var requestId = ++_refreshRequestId;
        IsLoading = true;
        Error = null;

        try
        {
            var balances = await Task.Run(() => _reserveService.GetBucketBalances());
            var movements = await Task.Run(() => _reserveService.GetMovementHistory());

            if (requestId != _refreshRequestId)
            {
                return;
            }

            ReplaceAll(Balances, balances);
            OnPropertyChanged(nameof(TotalBalance));

            ReplaceAll(Movements, ReserveMovementRow.BuildRows(movements));
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

    /// <summary>Closes all inline forms — only one form panel may be open at a time.</summary>
    private void CloseAllForms()
    {
        CloseSplitForm();
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

    internal async Task SubmitSplitAsync()
    {
        var validationMessage = IncomeSplitFormValidation.BuildValidationMessage(SplitDate, SplitAmount, SplitDescription);
        if (!string.IsNullOrEmpty(validationMessage))
        {
            SplitSaveError = validationMessage;
            return;
        }

        IsSubmittingSplit = true;
        SplitSaveError = null;

        try
        {
            var result = await _reserveService.PostIncomeSplitAsync(new IncomeSplitRequestDTO
            {
                Date = DateOnly.FromDateTime(SplitDate!.Value),
                Amount = decimal.Parse(SplitAmount),
                Description = SplitDescription,
            });

            LastSplitResult = result;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            SplitSaveError = ex.Message;
        }
        finally
        {
            IsSubmittingSplit = false;
        }
    }

    #endregion
}
