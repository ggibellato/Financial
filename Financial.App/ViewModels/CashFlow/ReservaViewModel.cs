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
}
