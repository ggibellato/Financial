using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class CardStatementService : ICardStatementService
{
    private const string EntityType = "CardStatement";

    private readonly ICashFlowRepository _repository;
    private readonly ILogger<CardStatementService> _logger;
    private readonly ITelemetryTracer _tracer;

    public CardStatementService(ICashFlowRepository repository, ILogger<CardStatementService> logger, ITelemetryTracer tracer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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

            var activeCards = _repository.GetCreditCards().Where(c => c.IsActive).ToList();
            if (activeCards.Count == 0)
            {
                _logger.LogWarning("No active credit cards found while generating statements for {Year}-{Month}.", year, month);
            }

            var created = false;
            foreach (var card in activeCards)
            {
                if (existingStatements.Any(s => s.CreditCard.Id == card.Id))
                {
                    continue;
                }

                var statement = CardStatement.Create(card, year, month);
                _repository.AddCardStatement(statement);
                existingStatements.Add(statement);
                created = true;
            }

            if (created)
            {
                await _repository.SaveChangesAsync().ConfigureAwait(false);
            }

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "GetStatementsForMonth");
            return existingStatements.Select(s => ToDto(s)).ToList();
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task<CardStatementDTO> MarkStatementPaidAsync(Guid id, MarkStatementPaidDTO request)
    {
        using var span = StartSpan("MarkStatementPaid");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var statement = FindStatementOrThrow(id);

            if (statement.IsPaid)
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "MarkStatementPaid");
                return ToDto(statement);
            }

            if (!EntityIdResolver.TryResolve(request.PaymentSourceBankId, _repository.GetBanks(), b => b.Id, out var bank))
            {
                throw new ArgumentException($"Payment source '{request.PaymentSourceBankId}' is not recognized.");
            }

            var settledAt = DateOnly.FromDateTime(DateTime.Today);
            var charges = GetStatementExpenses(statement, ExpensePaymentStatus.CreditCardCharge);
            // The warning text embeds the statement's invoice period, so it stays in the DTO/logs
            // only, never as a span attribute (logging-audit.md's note on this string).
            var warning = charges.Count == 0
                ? $"No credit card charges matched this statement's invoice period ({statement.Year:D4}-{statement.Month:D2}); marked paid with 0 linked charges."
                : null;

            statement.MarkPaid();
            foreach (var charge in charges)
            {
                charge.Settle(bank!, settledAt);
            }

            try
            {
                await _repository.SaveChangesAsync().ConfigureAwait(false);
            }
            catch
            {
                statement.MarkUnpaid();
                foreach (var charge in charges)
                {
                    charge.Unsettle();
                }

                throw;
            }

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "MarkStatementPaid");
            return ToDto(statement, warning);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
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
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "UnmarkStatementPaid");
                return ToDto(statement);
            }

            var settledExpenses = GetStatementExpenses(statement, ExpensePaymentStatus.CreditCardSettled);
            var settlements = settledExpenses
                .Select(e => (Expense: e, PaymentSourceBank: e.PaymentSourceBank!, PaymentDate: e.Date))
                .ToList();

            statement.MarkUnpaid();
            foreach (var expense in settledExpenses)
            {
                expense.Unsettle();
            }

            try
            {
                await _repository.SaveChangesAsync().ConfigureAwait(false);
            }
            catch
            {
                statement.MarkPaid();
                foreach (var (expense, paymentSourceBank, paymentDate) in settlements)
                {
                    expense.Settle(paymentSourceBank, paymentDate);
                }

                throw;
            }

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "UnmarkStatementPaid");
            return ToDto(statement);
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        var span = _tracer.StartSpan($"CashFlow.CardStatementService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "CashFlow");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }

    private CardStatement FindStatementOrThrow(Guid id) =>
        _repository.GetCardStatements().FirstOrThrow(s => s.Id == id, "Card statement", id);

    private List<Expense> GetStatementExpenses(CardStatement statement, ExpensePaymentStatus status) =>
        _repository.GetExpenses()
            .Where(e => e.CreditCard?.Id == statement.CreditCard.Id
                && e.InvoiceDate is not null
                && e.InvoiceDate.Value.Year == statement.Year
                && e.InvoiceDate.Value.Month == statement.Month
                && e.PaymentStatus == status)
            .ToList();

    private CardStatementDTO ToDto(CardStatement statement, string? warning = null) => new()
    {
        Id = statement.Id,
        CreditCardId = statement.CreditCard.Id,
        CreditCardName = statement.CreditCard.Name,
        Year = statement.Year,
        Month = statement.Month,
        IsPaid = statement.IsPaid,
        OutstandingTotal = GetStatementExpenses(statement, ExpensePaymentStatus.CreditCardCharge).Sum(e => e.Value),
        Warning = warning
    };
}
