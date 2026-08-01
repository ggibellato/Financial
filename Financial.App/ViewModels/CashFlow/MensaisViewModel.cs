using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// ViewModel for the Mensais tab: recurring bills split into Brasil/UK tables, Add Bill,
/// Edit Bill (Value/Status), delete, and Reset All to Unset. Mirrors Financial.Web's
/// useMensais.ts hook. Standalone from <see cref="MonthlyViewModel"/>/<see cref="ReservaViewModel"/>
/// — Mensais is its own top-level destination with no shared state.
///
/// The Month+Year picker is display-only: RecurringBillDTO has no year/month field, and
/// GetBills() is not filterable by period — the picker never affects what RefreshAsync loads.
/// This matches useMensais.ts's own monthInputValue, which has zero effect on getMensaisBills().
/// </summary>
public class MensaisViewModel : ViewModelBase
{
    private readonly IMensaisService _mensaisService;
    private readonly Func<string, bool> _confirm;

    private int _displayYear;
    private int _displayMonth;
    private bool _isLoading = true;
    private string? _error;

    public int DisplayYear
    {
        get => _displayYear;
        set => SetProperty(ref _displayYear, value);
    }

    public int DisplayMonth
    {
        get => _displayMonth;
        set => SetProperty(ref _displayMonth, value);
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

    public ObservableCollection<RecurringBillDTO> BrasilBills { get; } = [];
    public ObservableCollection<RecurringBillDTO> UkBills { get; } = [];

    public RelayCommand RetryCommand { get; }

    public MensaisViewModel(IMensaisService mensaisService, Func<string, bool> confirm)
    {
        _mensaisService = mensaisService ?? throw new ArgumentNullException(nameof(mensaisService));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));

        var today = DateTime.Today;
        _displayYear = today.Year;
        _displayMonth = today.Month;

        RetryCommand = new RelayCommand(async () => await RefreshAsync());

        _ = RefreshAsync();
    }

    private int _refreshRequestId;

    /// <summary>
    /// Reloads all bills and splits them into Brasil/UK. Guards against overlapping calls
    /// (e.g. the constructor's initial load racing a manual retry) by discarding a completion
    /// whose request has been superseded.
    /// </summary>
    internal async Task RefreshAsync()
    {
        var requestId = ++_refreshRequestId;
        IsLoading = true;
        Error = null;

        try
        {
            var bills = await Task.Run(() => _mensaisService.GetBills());

            if (requestId != _refreshRequestId)
            {
                return;
            }

            ApplyBills(bills);
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

    private void ApplyBills(IReadOnlyList<RecurringBillDTO> bills)
    {
        ReplaceAll(BrasilBills, bills.Where(b => b.Area == "Brasil"));
        ReplaceAll(UkBills, bills.Where(b => b.Area == "UK"));
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
