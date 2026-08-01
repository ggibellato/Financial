using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// ViewModel for the Controle Mãe tab: a BRL/GBP ledger filtered by a "From" date, an
/// all-time totals row, Create Entry (single-currency input, backend FX conversion), Edit
/// Entry (direct BRL/GBP override), and delete. Mirrors Financial.Web's useControleMae.ts
/// hook. Standalone from the other CashFlow ViewModels — its own top-level destination.
///
/// Totals are all-time (IControleMaeService.GetTotals() has no date parameter) and are
/// fetched independently of FromDate — confirmed with the user to match the web reference's
/// actual behavior (its totals useEffect has no fromDate dependency, and the totals row is
/// literally labeled "Total (all entries)"), not the PRD's slightly imprecise wording.
/// </summary>
public class ControleMaeViewModel : ViewModelBase
{
    public static readonly string[] Currencies = ["BRL", "GBP"];

    private readonly IControleMaeService _controleMaeService;
    private readonly Func<string, bool> _confirm;

    private DateTime _fromDate;
    private bool _isLoading = true;
    private string? _error;
    private MaeLedgerTotalsDTO? _totals;

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
            {
                _ = RefreshEntriesAsync();
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

    public MaeLedgerTotalsDTO? Totals
    {
        get => _totals;
        private set => SetProperty(ref _totals, value);
    }

    public ObservableCollection<MaeLedgerEntryDTO> Entries { get; } = [];

    public RelayCommand RetryCommand { get; }

    public ControleMaeViewModel(IControleMaeService controleMaeService, Func<string, bool> confirm)
    {
        _controleMaeService = controleMaeService ?? throw new ArgumentNullException(nameof(controleMaeService));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));

        _fromDate = new DateTime(DateTime.Today.Year - 1, 1, 1);

        RetryCommand = new RelayCommand(async () =>
        {
            await RefreshEntriesAsync();
            await RefreshTotalsAsync();
        });

        _ = RefreshEntriesAsync();
        _ = RefreshTotalsAsync();
    }

    private int _refreshEntriesRequestId;

    /// <summary>
    /// Reloads the ledger entries on/after FromDate. Guards against overlapping calls (e.g.
    /// the constructor's initial load racing a rapid FromDate change) by discarding a
    /// completion whose request has been superseded.
    /// </summary>
    internal async Task RefreshEntriesAsync()
    {
        var requestId = ++_refreshEntriesRequestId;
        IsLoading = true;
        Error = null;

        try
        {
            var fromDate = DateOnly.FromDateTime(FromDate);
            var entries = await Task.Run(() => _controleMaeService.GetEntriesFromDate(fromDate));

            if (requestId != _refreshEntriesRequestId)
            {
                return;
            }

            ReplaceAll(Entries, entries);
        }
        catch (Exception ex)
        {
            if (requestId == _refreshEntriesRequestId)
            {
                Error = ex.Message;
            }
        }
        finally
        {
            if (requestId == _refreshEntriesRequestId)
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>
    /// Reloads the all-time totals. Deliberately independent of FromDate — see class summary.
    /// A failure here is non-fatal (matches the web: "totals are supplementary to the ledger
    /// list; a failed refresh just keeps the last known values").
    /// </summary>
    internal async Task RefreshTotalsAsync()
    {
        try
        {
            Totals = await Task.Run(() => _controleMaeService.GetTotals());
        }
        catch
        {
            // Supplementary data; keep the last known totals on failure.
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
