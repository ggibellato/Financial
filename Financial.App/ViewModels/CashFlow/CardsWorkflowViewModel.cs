using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class CardsWorkflowViewModel : ViewModelBase
{
    private readonly ICardStatementService _cardStatementService;
    private readonly ICreditCardService _creditCardService;
    private readonly ObservableCollection<CreditCardDTO> _creditCards;
    private readonly Func<Task> _refresh;

    private string? _cardStatementError;
    private string? _cardStatementWarning;
    private string? _creditCardUpdateError;
    private Guid? _updatingCreditCardId;

    public ObservableCollection<CardStatementDTO> CardStatements { get; } = [];

    /// <summary>The same instance MonthlyViewModel owns — mutated in place by its refresh, never replaced.</summary>
    public ObservableCollection<BankDTO> Banks { get; }

    public IEnumerable<CreditCardManagementRow> CreditCardManagementRows =>
        _creditCards.Select(c => new CreditCardManagementRow
        {
            CreditCard = c,
            Statement = CardStatements.FirstOrDefault(s => s.CreditCardId == c.Id),
        });

    public IEnumerable<CreditCardManagementRow> FilteredCreditCardManagementRows => CreditCardManagementRows.Where(CardFilter.Matches);

    public ColumnFilterViewModel<CreditCardManagementRow> CardFilter { get; }

    public decimal AdjustmentTotal => CardStatements.Sum(s => s.OutstandingTotal);

    public string? CardStatementError
    {
        get => _cardStatementError;
        private set => SetProperty(ref _cardStatementError, value);
    }

    /// <summary>
    /// A completed call that changed nothing - marking a statement paid that already was, say.
    /// Separate from <see cref="CardStatementError"/> because it is not a failure, and showing it
    /// in red would teach the user to ignore red. Mirrors useMonthly.ts's listActionWarning.
    /// </summary>
    public string? CardStatementWarning
    {
        get => _cardStatementWarning;
        private set => SetProperty(ref _cardStatementWarning, value);
    }

    public string? CreditCardUpdateError
    {
        get => _creditCardUpdateError;
        private set => SetProperty(ref _creditCardUpdateError, value);
    }

    public Guid? UpdatingCreditCardId
    {
        get => _updatingCreditCardId;
        private set => SetProperty(ref _updatingCreditCardId, value);
    }

    public Dictionary<Guid, Guid> MarkPaidSources { get; } = [];

    public RelayCommand<CardStatementDTO> MarkStatementPaidCommand { get; }
    public RelayCommand<CardStatementDTO> UnmarkStatementPaidCommand { get; }

    public CardsWorkflowViewModel(
        ICardStatementService cardStatementService,
        ICreditCardService creditCardService,
        ObservableCollection<BankDTO> banks,
        ObservableCollection<CreditCardDTO> creditCards,
        Func<Task> refresh)
    {
        _cardStatementService = cardStatementService ?? throw new ArgumentNullException(nameof(cardStatementService));
        _creditCardService = creditCardService ?? throw new ArgumentNullException(nameof(creditCardService));
        Banks = banks ?? throw new ArgumentNullException(nameof(banks));
        _creditCards = creditCards ?? throw new ArgumentNullException(nameof(creditCards));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        MarkStatementPaidCommand = new RelayCommand<CardStatementDTO>(
            async statement => await MarkStatementPaidAsync(statement),
            statement => statement != null && MarkPaidSources.ContainsKey(statement.Id));
        UnmarkStatementPaidCommand = new RelayCommand<CardStatementDTO>(async statement => await UnmarkStatementPaidAsync(statement));

        CardFilter = new ColumnFilterViewModel<CreditCardManagementRow>("Card", row => [row.CreditCardName], NotifyCardFilterChanged);
    }

    private void NotifyCardFilterChanged() => OnPropertyChanged(nameof(FilteredCreditCardManagementRows));

    /// <summary>Applies data the coordinator's own refresh already fetched — this workflow never fetches on its own.</summary>
    public void ApplyRefresh(IReadOnlyList<CardStatementDTO> cardStatements)
    {
        ReplaceAll(CardStatements, cardStatements);
        OnPropertyChanged(nameof(AdjustmentTotal));
    }

    /// <summary>CreditCards is the coordinator's own shared collection, mutated in place - it calls
    /// this after replacing it so CreditCardManagementRows re-queries, mirroring how it used to notify itself.</summary>
    internal void NotifyCreditCardsChanged()
    {
        OnPropertyChanged(nameof(CreditCardManagementRows));
        CardFilter.Refresh(CreditCardManagementRows);
        OnPropertyChanged(nameof(FilteredCreditCardManagementRows));
    }

    public void SetMarkPaidSource(Guid statementId, Guid bankId)
    {
        MarkPaidSources[statementId] = bankId;
        MarkStatementPaidCommand.RaiseCanExecuteChanged();
    }

    internal async Task MarkStatementPaidAsync(CardStatementDTO? statement)
    {
        if (statement is null || !MarkPaidSources.TryGetValue(statement.Id, out var paymentSource))
        {
            return;
        }

        CardStatementError = null;
        CardStatementWarning = null;

        try
        {
            var result = await _cardStatementService.MarkStatementPaidAsync(statement.Id, new MarkCardStatementPaidDTO { PaymentSourceBankId = paymentSource });
            CardStatementWarning = result.Warning;
            MarkPaidSources.Remove(statement.Id);
            await _refresh();
        }
        catch (Exception ex)
        {
            CardStatementError = ex.Message;
        }
    }

    internal async Task UnmarkStatementPaidAsync(CardStatementDTO? statement)
    {
        if (statement is null)
        {
            return;
        }

        CardStatementError = null;
        CardStatementWarning = null;

        try
        {
            var result = await _cardStatementService.UnmarkStatementPaidAsync(statement.Id);
            CardStatementWarning = result.Warning;
            await _refresh();
        }
        catch (Exception ex)
        {
            CardStatementError = ex.Message;
        }
    }

    internal async Task UpdateCreditCardAsync(CreditCardDTO? card, DateOnly? nextInvoiceDueDate, bool isActive)
    {
        if (card is null)
        {
            return;
        }

        // The grid's DatePicker/CheckBox bind one-way to this row and call back here on their
        // change events - but WPF raises those same events when ReplaceAll(CreditCards, ...)
        // below rebinds the row to its own current value, not just on a real user edit. Without
        // this guard, that echo would call the update service and refresh again, which rebinds
        // the row again, forever.
        if (card.NextInvoiceDueDate == nextInvoiceDueDate && card.IsActive == isActive)
        {
            return;
        }

        CreditCardUpdateError = null;
        UpdatingCreditCardId = card.Id;

        try
        {
            await _creditCardService.UpdateCreditCardAsync(card.Id, new CreditCardUpdateDTO
            {
                NextInvoiceDueDate = nextInvoiceDueDate,
                IsActive = isActive,
            });
            await _refresh();
        }
        catch (Exception ex)
        {
            CreditCardUpdateError = ex.Message;
        }
        finally
        {
            UpdatingCreditCardId = null;
        }
    }
}
