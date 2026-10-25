using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class CardStatementService : ICardStatementService
{
    private const string EntityType = "CardStatement";

    private readonly ICashFlowRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CardStatementService> _logger;
    private readonly ITelemetryTracer _tracer;

    public CardStatementService(ICashFlowRepository repository, TimeProvider timeProvider, ILogger<CardStatementService> logger, ITelemetryTracer tracer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    public async Task<IReadOnlyList<CardStatementDTO>> GetStatementsForMonthAsync(int year, int month)
    {
        using var span = StartSpan("GetStatementsForMonth");
        try
        {
            var existingStatements = _repository.GetCardStatements()
                .Where(s => s.Year == year && s.Month == month)
                .ToList();

            var activeCards = _repository.GetCreditCardAccounts().Where(c => c.IsActive).ToList();
            if (activeCards.Count == 0)
            {
                _logger.LogWarning("No active credit cards found while generating statements for {Year}-{Month}.", year, month);
            }

            await _repository.ApplyAndSaveAsync(() =>
            {
                var created = false;
                foreach (var card in activeCards)
                {
                    if (existingStatements.Any(s => s.IsFor(card.Id, year, month)))
                    {
                        continue;
                    }

                    var statement = CardStatement.Create(card, year, month);
                    _repository.AddCardStatement(statement);
                    existingStatements.Add(statement);
                    created = true;
                }

                return created;
            }).ConfigureAwait(false);

            var expenses = _repository.GetExpenses().ToList();
            var records = AccountTransactionReader.Read(_repository);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetStatementsForMonth");
            return existingStatements
                .Select(s => ToDto(s, OutstandingTotal(expenses, s), AccumulatedOutstandingTotal(s.CreditCard, records), warning: null))
                .ToList();
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CardStatementDTO> MarkStatementPaidAsync(Guid id, MarkCardStatementPaidDTO request)
    {
        using var span = StartSpan("MarkStatementPaid");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var statement = FindStatementOrThrow(id);

            if (statement.IsPaid)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "MarkStatementPaid");
                return ToDto(statement, "This statement was already marked paid; nothing changed.");
            }

            if (!EntityIdResolver.TryResolve(request.PaymentSourceBankId, _repository.GetAccounts(), b => b.Id, out var bank))
            {
                throw new ArgumentException($"Payment source '{request.PaymentSourceBankId}' is not recognized.");
            }

            Expense.EnsureCanBePaymentSource(bank!);

            var settledAt = _timeProvider.GetLocalToday();
            var charges = UnpaidCharges(_repository.GetExpenses(), statement);
            // The warning text embeds the statement's invoice period, so it stays in the DTO/logs
            // only, never as a span attribute (logging-audit.md's note on this string).
            var warning = charges.Count == 0
                ? $"No credit card charges matched this statement's invoice period ({statement.Year:D4}-{statement.Month:D2}); marked paid with 0 linked charges."
                : null;

            await _repository.ApplyAndSaveAsync(() =>
            {
                statement.MarkPaid();
                foreach (var charge in charges)
                {
                    charge.Settle(bank!, settledAt);
                }

                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "MarkStatementPaid");
            return ToDto(statement, warning);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CardStatementDTO> UnmarkStatementPaidAsync(Guid id)
    {
        using var span = StartSpan("UnmarkStatementPaid");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            var statement = FindStatementOrThrow(id);

            if (!statement.IsPaid)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "UnmarkStatementPaid");
                return ToDto(statement, "This statement was not marked paid; nothing changed.");
            }

            var settledExpenses = CardChargeFilter.ChargesFor(_repository.GetExpenses(), statement.CreditCard, statement.Year, statement.Month, includePaid: true)
                .Where(e => e.PaymentStatus == ExpensePaymentStatus.CreditCardSettled)
                .ToList();

            await _repository.ApplyAndSaveAsync(() =>
            {
                statement.MarkUnpaid();
                foreach (var expense in settledExpenses)
                {
                    expense.Unsettle();
                }

                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UnmarkStatementPaid");
            return ToDto(statement);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public (decimal Total, bool HasChargesPosted) GetOutstandingTotalForPeriod(Guid creditCardId, int year, int month)
    {
        var card = _repository.GetCreditCardAccounts().FirstOrDefault(c => c.Id == creditCardId);
        if (card is null)
        {
            return (0m, false);
        }

        var charges = CardChargeFilter.ChargesFor(_repository.GetExpenses(), card, year, month, includePaid: false).ToList();
        return (charges.Sum(e => e.Value), charges.Count > 0);
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(CardStatementService), operationName, EntityType);
    }

    private CardStatement FindStatementOrThrow(Guid id) =>
        _repository.GetCardStatements().FirstOrThrow(s => s.Id == id, "Card statement", id);

    private static List<Expense> UnpaidCharges(IEnumerable<Expense> expenses, CardStatement statement) =>
        CardChargeFilter.ChargesFor(expenses, statement.CreditCard, statement.Year, statement.Month, includePaid: false).ToList();

    private static decimal OutstandingTotal(IEnumerable<Expense> expenses, CardStatement statement) =>
        UnpaidCharges(expenses, statement).Sum(e => e.Value);

    private static decimal AccumulatedOutstandingTotal(Account card, IEnumerable<IAccountTransaction> records) =>
        -AccountBalanceRule.Balance(card, DateOnly.MaxValue, records);

    private CardStatementDTO ToDto(CardStatement statement, string? warning = null) =>
        ToDto(
            statement,
            OutstandingTotal(_repository.GetExpenses(), statement),
            AccumulatedOutstandingTotal(statement.CreditCard, AccountTransactionReader.Read(_repository)),
            warning);

    private static CardStatementDTO ToDto(CardStatement statement, decimal outstandingTotal, decimal accumulatedOutstandingTotal, string? warning) => new()
    {
        Id = statement.Id,
        CreditCardId = statement.CreditCard.Id,
        CreditCardName = statement.CreditCard.Name,
        Year = statement.Year,
        Month = statement.Month,
        IsPaid = statement.IsPaid,
        OutstandingTotal = outstandingTotal,
        AccumulatedOutstandingTotal = accumulatedOutstandingTotal,
        Warning = warning
    };
}
