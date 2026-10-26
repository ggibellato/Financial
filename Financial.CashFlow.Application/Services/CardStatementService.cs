using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
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
    private const string PaymentChangedElsewhereMessage = "The payment for this statement was changed elsewhere. Reload and try again.";

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

    public IReadOnlyList<CardStatementDTO> GetStatementsForMonth(int year, int month)
    {
        using var span = StartSpan("GetStatementsForMonth");
        try
        {
            var statements = _repository.GetCardStatements().ToList();
            var existingStatements = statements
                .Where(s => s.Year == year && s.Month == month)
                .ToList();

            var expenses = _repository.GetExpenses().ToList();
            var records = AccountTransactionReader.Read(_repository);
            var accumulatedByCard = existingStatements
                .Select(s => s.CreditCard)
                .Distinct()
                .ToDictionary(card => card.Id, card => AccumulatedOutstandingTotal(card, records));

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetStatementsForMonth");
            return existingStatements
                .Select(s => ToDto(s, OutstandingTotal(expenses, statements, s), accumulatedByCard[s.CreditCard.Id]))
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
                throw new StatementPaymentConflictException("This statement is already paid.");
            }

            if (!EntityIdResolver.TryResolve(request.PaymentSourceBankId, _repository.GetAccounts(), b => b.Id, out var bank))
            {
                throw new ArgumentException($"Payment source '{request.PaymentSourceBankId}' is not recognized.");
            }

            bank!.EnsureCanPay();

            var total = CardChargeFilter.OutstandingTotal(
                _repository.GetExpenses(), _repository.GetCardStatements(), statement.CreditCard, statement.Year, statement.Month);
            if (total <= 0)
            {
                throw new ArgumentException("There is nothing to pay on this statement.");
            }

            var today = _timeProvider.GetLocalToday();
            var payment = Transfer.CreateStatementPayment(request.Date ?? today, bank!, statement.CreditCard, total, today);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddTransfer(payment);
                statement.MarkPaidBy(payment.Id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "MarkStatementPaid");
            return ToDto(statement);
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
                throw new StatementPaymentConflictException("This statement is not paid.");
            }

            await _repository.ApplyAndSaveAsync(statement.IsPaidByTransfer ? TransferPaymentReversal(statement) : LegacyPaymentReversal(statement))
                .ConfigureAwait(false);

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

        var charges = CardChargeFilter.UnpaidChargesFor(_repository.GetExpenses(), _repository.GetCardStatements(), card, year, month).ToList();
        return (charges.Sum(e => e.Value), charges.Count > 0);
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(CardStatementService), operationName, EntityType);
    }

    private CardStatement FindStatementOrThrow(Guid id) =>
        _repository.GetCardStatements().FirstOrThrow(s => s.Id == id, "Card statement", id);

    private Func<bool> TransferPaymentReversal(CardStatement statement)
    {
        var paymentId = statement.PaymentTransferId!.Value;
        if (!_repository.GetTransfers().Any(transfer => transfer.Id == paymentId))
        {
            throw new StatementPaymentConflictException(PaymentChangedElsewhereMessage);
        }

        return () =>
        {
            _repository.DeleteTransfer(paymentId);
            statement.MarkUnpaid();
            return true;
        };
    }

    private Func<bool> LegacyPaymentReversal(CardStatement statement)
    {
        var settledCharges = CardChargeFilter.ChargesFor(_repository.GetExpenses(), statement.CreditCard, statement.Year, statement.Month)
            .Where(e => e.PaymentStatus == ExpensePaymentStatus.CreditCardSettled)
            .ToList();

        return () =>
        {
            statement.MarkUnpaid();
            foreach (var charge in settledCharges)
            {
                charge.Unsettle();
            }

            return true;
        };
    }

    private static decimal OutstandingTotal(IEnumerable<Expense> expenses, IEnumerable<CardStatement> statements, CardStatement statement) =>
        CardChargeFilter.OutstandingTotal(expenses, statements, statement.CreditCard, statement.Year, statement.Month);

    private static decimal AccumulatedOutstandingTotal(Account card, IEnumerable<IAccountTransaction> records) =>
        -AccountBalanceRule.Balance(card, DateOnly.MaxValue, records);

    private CardStatementDTO ToDto(CardStatement statement) =>
        ToDto(
            statement,
            OutstandingTotal(_repository.GetExpenses(), _repository.GetCardStatements(), statement),
            AccumulatedOutstandingTotal(statement.CreditCard, AccountTransactionReader.Read(_repository)));

    private static CardStatementDTO ToDto(CardStatement statement, decimal outstandingTotal, decimal accumulatedOutstandingTotal) => new()
    {
        Id = statement.Id,
        CreditCardId = statement.CreditCard.Id,
        CreditCardName = statement.CreditCard.Name,
        Year = statement.Year,
        Month = statement.Month,
        IsPaid = statement.IsPaid,
        OutstandingTotal = outstandingTotal,
        AccumulatedOutstandingTotal = accumulatedOutstandingTotal
    };
}
