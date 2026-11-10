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

            var ledgerMonth = CardStatementLedger.MonthOf(
                statement.CreditCard, _repository.GetExpenses(), _repository.GetCardStatements(), _repository.GetTransfers(), statement.Year, statement.Month);
            if (ledgerMonth is not { IsUnpaid: true })
            {
                throw new ArgumentException("There is nothing to pay on this statement.");
            }

            var today = _timeProvider.GetLocalToday();
            var payment = Transfer.CreateStatementPayment(request.Date ?? today, bank!, statement.CreditCard, ledgerMonth.AmountToPay, today);

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

    public decimal? GetAmountDueForPeriod(Guid creditCardId, int year, int month)
    {
        using var span = StartSpan("GetAmountDueForPeriod");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, creditCardId.ToString());
        try
        {
            var card = _repository.GetCreditCardAccounts().FirstOrDefault(c => c.Id == creditCardId);
            var ledgerMonth = card is null
                ? null
                : CardStatementLedger.MonthOf(card, _repository.GetExpenses(), _repository.GetCardStatements(), _repository.GetTransfers(), year, month);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetAmountDueForPeriod");
            return ledgerMonth is null or { Status: CardStatementLedgerStatus.NothingDue } ? null : ledgerMonth.AmountToPay;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
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
        var settledCharges = CardChargeFilter.SettledChargesFor(_repository.GetExpenses(), statement.CreditCard, statement.Year, statement.Month)
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

    private CardStatementDTO ToDto(CardStatement statement) =>
        ToDto(
            statement,
            AmountToPay(statement, _repository.GetExpenses(), _repository.GetCardStatements(), _repository.GetTransfers()),
            CardChargeFilter.TotalOwed(statement.CreditCard, AccountTransactionReader.Read(_repository)));

    private static decimal AmountToPay(
        CardStatement statement, IEnumerable<Expense> expenses, IEnumerable<CardStatement> statements, IEnumerable<Transfer> transfers) =>
        CardStatementLedger.MonthOf(statement.CreditCard, expenses, statements, transfers, statement.Year, statement.Month)?.AmountToPay ?? 0m;

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
