using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class BankOperationsWorkflowViewModel : ViewModelBase
{
    public const string AllBanksFilter = "All Banks";

    private readonly ITransferService _transferService;
    private readonly IBalanceAdjustmentService _balanceAdjustmentService;
    private readonly Func<string, bool> _confirm;
    private readonly Func<Task> _refresh;

    private string? _bankOperationsError;
    private string _selectedBankFilter = AllBanksFilter;
    private IReadOnlyList<string> _bankFilterOptions = [AllBanksFilter];

    public TransferWorkflowViewModel Transfer { get; }
    public AdjustmentWorkflowViewModel Adjustment { get; }

    public ObservableCollection<BankOperationRow> BankOperations { get; } = [];
    public ObservableCollection<BankOperationRow> FilteredBankOperations { get; } = [];

    public bool HasBankOperations => FilteredBankOperations.Count > 0;

    public string BankOperationsEmptyMessage => SelectedBankFilter == AllBanksFilter
        ? "No transfers or balance corrections this month."
        : $"No transfers or balance corrections for {SelectedBankFilter} this month.";

    public string? BankOperationsError
    {
        get => _bankOperationsError;
        private set => SetProperty(ref _bankOperationsError, value);
    }

    public string SelectedBankFilter
    {
        get => _selectedBankFilter;
        set
        {
            if (SetProperty(ref _selectedBankFilter, value))
            {
                OnPropertyChanged(nameof(BankOperationsEmptyMessage));
                ApplyBankFilter();
            }
        }
    }

    public IReadOnlyList<string> BankFilterOptions
    {
        get => _bankFilterOptions;
        private set => SetProperty(ref _bankFilterOptions, value);
    }

    public RelayCommand<BankOperationRow> DeleteBankOperationCommand { get; }

    public BankOperationsWorkflowViewModel(
        ITransferService transferService,
        IBalanceAdjustmentService balanceAdjustmentService,
        ObservableCollection<BankDTO> banks,
        ObservableCollection<BankTotalRow> bankTotals,
        Func<string, bool> confirm,
        Func<Task> refresh)
    {
        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
        _balanceAdjustmentService = balanceAdjustmentService ?? throw new ArgumentNullException(nameof(balanceAdjustmentService));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        Transfer = new TransferWorkflowViewModel(transferService, banks, refresh);
        Adjustment = new AdjustmentWorkflowViewModel(balanceAdjustmentService, banks, bankTotals, refresh);

        DeleteBankOperationCommand = new RelayCommand<BankOperationRow>(async row => await DeleteBankOperationAsync(row));
    }

    /// <summary>Applies data the coordinator's own refresh already fetched — this workflow never fetches on its own.</summary>
    public void ApplyRefresh(
        IReadOnlyList<TransferDTO> transfers,
        IReadOnlyList<IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank,
        int year,
        int month,
        IReadOnlyList<BankDTO> banks)
    {
        ReplaceAll(BankOperations, BuildBankOperations(transfers, adjustmentsByBank, year, month));
        BankFilterOptions = BuildBankFilterOptions(banks);
        ApplyBankFilter();
    }

    private static List<BankOperationRow> BuildBankOperations(
        IReadOnlyList<TransferDTO> transfers,
        IReadOnlyList<IReadOnlyList<BalanceAdjustmentDTO>> adjustmentsByBank,
        int year,
        int month)
    {
        // Transfers are already month-scoped by ITransferService.GetTransfersByMonth; only
        // adjustments (fetched per bank via GetAdjustmentsByBank, not month-scoped) need filtering here.
        var rows = new List<BankOperationRow>(transfers.Select(BankOperationRow.FromTransfer));

        rows.AddRange(adjustmentsByBank
            .SelectMany(adjustments => adjustments)
            .Where(a => a.Date.Year == year && a.Date.Month == month)
            .Select(BankOperationRow.FromAdjustment));

        return rows.OrderByDescending(r => r.Date).ToList();
    }

    /// <summary>Options for the Bank tab's filter dropdown: "All Banks" plus each configured bank name.</summary>
    private static IReadOnlyList<string> BuildBankFilterOptions(IReadOnlyList<BankDTO> banks) =>
        new[] { AllBanksFilter }.Concat(banks.Select(b => b.Name)).ToList();

    private void ApplyBankFilter()
    {
        var matching = SelectedBankFilter == AllBanksFilter
            ? BankOperations
            : BankOperations.Where(row => row.MatchesBank(SelectedBankFilter));

        ReplaceAll(FilteredBankOperations, matching);
        OnPropertyChanged(nameof(HasBankOperations));
    }

    internal async Task DeleteBankOperationAsync(BankOperationRow? row)
    {
        if (row is null)
        {
            return;
        }

        var confirmMessage = row.Kind == BankOperationKind.Adjustment
            ? "Delete this balance adjustment? This removes it for good."
            : "Delete this transfer? This removes it for good.";

        if (!_confirm(confirmMessage))
        {
            return;
        }

        BankOperationsError = null;

        try
        {
            if (row.Transfer is { } transfer)
            {
                await _transferService.DeleteTransferAsync(transfer.Id);
            }
            else if (row.Adjustment is { } adjustment)
            {
                await _balanceAdjustmentService.DeleteAdjustmentAsync(adjustment.BankId, adjustment.Id);
            }

            await _refresh();
        }
        catch (Exception ex)
        {
            BankOperationsError = ex.Message;
        }
    }
}
