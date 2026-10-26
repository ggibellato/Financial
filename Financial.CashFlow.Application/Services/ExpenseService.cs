using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class ExpenseService : IExpenseService
{
    private const string EntityType = "Expense";
    private const string PaidStatementMessage = "This charge's statement is paid; unmark it first.";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<ExpenseService> _logger;
    private readonly ICreditCardCalendarSyncService _calendarSync;

    public ExpenseService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<ExpenseService> logger, ICreditCardCalendarSyncService calendarSync)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _calendarSync = calendarSync ?? throw new ArgumentNullException(nameof(calendarSync));
    }

    public async Task<ExpenseDTO> AddExpenseAsync(ExpenseCreateDTO request)
    {
        using var span = StartSpan("AddExpense");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var (category, paymentSource, creditCard) = ValidateFields(
                request.Description, request.CategoryId, request.PaymentSourceBankId, request.CreditCardId);

            var expense = Expense.Create(request.Date, request.Description, request.Value, category, paymentSource, creditCard, request.InvoiceDate, request.CountsAsTithe);
            expense.SetRoundUpAmount(request.RoundUpAmount);
            EnsureStatementNotPaid(expense.CreditCard, expense.InvoiceDate);
            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddExpense(expense);
                CardStatementCreation.EnsureFor(_repository, expense);
                return true;
            }).ConfigureAwait(false);

            NotifyInvoiceChanged(expense.CreditCard?.Id, expense.InvoiceDate);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, expense.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddExpense");
            return ToDto(expense);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<ExpenseDTO> UpdateExpenseAsync(Guid id, ExpenseUpdateDTO request)
    {
        using var span = StartSpan("UpdateExpense");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var expense = FindExpenseOrThrow(id);

            var (category, paymentSource, creditCard) = ValidateFields(
                request.Description, request.CategoryId, request.PaymentSourceBankId, request.CreditCardId);
            EnsurePaidStatementLocksHold(expense, request, paymentSource, creditCard);
            var previousCardId = expense.CreditCard?.Id;
            var previousInvoiceDate = expense.InvoiceDate;

            await _repository.ApplyAndSaveAsync(() =>
            {
                expense.UpdateDetails(request.Date, request.Description, request.Value, category, paymentSource, creditCard, request.CountsAsTithe);
                expense.SetRoundUpAmount(request.RoundUpAmount);

                if (request.InvoiceDate is not null && request.InvoiceDate != expense.InvoiceDate)
                {
                    expense.SetInvoiceDate(request.InvoiceDate.Value);
                }

                CardStatementCreation.EnsureFor(_repository, expense);
                return true;
            }).ConfigureAwait(false);

            NotifyInvoiceChanged(previousCardId, previousInvoiceDate);
            if (previousCardId != expense.CreditCard?.Id || previousInvoiceDate != expense.InvoiceDate)
            {
                NotifyInvoiceChanged(expense.CreditCard?.Id, expense.InvoiceDate);
            }

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateExpense");
            return ToDto(expense);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteExpenseAsync(Guid id)
    {
        using var span = StartSpan("DeleteExpense");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            var expense = FindExpenseOrThrow(id);
            if (CardChargeFilter.IsOnPaidStatement(expense, _repository.GetCardStatements()))
            {
                throw new ArgumentException(PaidStatementMessage);
            }

            var cardId = expense.CreditCard?.Id;
            var invoiceDate = expense.InvoiceDate;

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteExpense(id);
                return true;
            }).ConfigureAwait(false);

            NotifyInvoiceChanged(cardId, invoiceDate);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteExpense");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<ExpenseDTO> GetExpensesByMonth(int year, int month)
    {
        using var span = StartSpan("GetExpensesByMonth");
        try
        {
            var result = _repository.GetExpenses()
                .Where(e => e.ListingDate.Year == year && e.ListingDate.Month == month)
                .Where(e => e.PaymentStatus != ExpensePaymentStatus.CreditCardCharge)
                .OrderByDescending(e => e.ListingDate)
                .Select(ToDto)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetExpensesByMonth");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<ExpenseDTO> GetUnpaidCardChargesByMonth(int year, int month)
    {
        using var span = StartSpan("GetUnpaidCardChargesByMonth");
        try
        {
            var result = _repository.GetExpenses()
                .Where(e => e.ListingDate.Year == year && e.ListingDate.Month == month)
                .Where(e => e.PaymentStatus == ExpensePaymentStatus.CreditCardCharge)
                // Every unpaid charge in a given month shares the same InvoiceDate (the 1st of that
                // month), so ordering by ListingDate here is a no-op tie that leaves rows in
                // repository storage order. Order by the actual charge date instead.
                .OrderByDescending(e => e.ChargeDate ?? e.Date)
                .Select(ToDto)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetUnpaidCardChargesByMonth");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<CategoryTotalDTO> GetCategoryTotalsByMonth(int year, int month)
    {
        using var span = StartSpan("GetCategoryTotalsByMonth");
        try
        {
            var result = _repository.GetExpenses()
                .Where(e => e.ReportingDate.Year == year && e.ReportingDate.Month == month)
                .GroupBy(e => e.Category.Name)
                .Select(g => new CategoryTotalDTO
                {
                    Category = g.Key,
                    TotalValue = g.Sum(e => e.Value)
                })
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetCategoryTotalsByMonth");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private void NotifyInvoiceChanged(Guid? creditCardId, DateOnly? invoiceDate)
    {
        if (creditCardId is not null && invoiceDate is not null)
        {
            _calendarSync.TriggerSyncIfInvoiceSynced(creditCardId.Value, invoiceDate.Value);
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(ExpenseService), operationName, EntityType);
    }

    private Expense FindExpenseOrThrow(Guid id) =>
        _repository.GetExpenses().FirstOrThrow(e => e.Id == id, "Expense", id);

    private void EnsurePaidStatementLocksHold(Expense expense, ExpenseUpdateDTO request, Account? paymentSource, Account? creditCard)
    {
        var targetInvoiceDate = creditCard is not null && request.InvoiceDate is { } requested
            ? FirstOfMonth(requested)
            : expense.InvoiceDateAfterUpdate(request.Date, creditCard);
        var movesStatement = creditCard?.Id != expense.CreditCard?.Id || targetInvoiceDate != expense.InvoiceDate;

        if (CardChargeFilter.IsOnPaidStatement(expense, _repository.GetCardStatements())
            && (movesStatement
                || request.Date != expense.Date
                || request.Value != expense.Value
                || paymentSource?.Id != expense.PaymentSourceAccount?.Id
                || request.RoundUpAmount != expense.RoundUpAmount))
        {
            throw new ArgumentException(PaidStatementMessage);
        }

        if (movesStatement)
        {
            EnsureStatementNotPaid(creditCard, targetInvoiceDate);
        }
    }

    private void EnsureStatementNotPaid(Account? creditCard, DateOnly? invoiceDate)
    {
        if (creditCard is not null
            && invoiceDate is { } date
            && CardChargeFilter.IsStatementPaid(_repository.GetCardStatements(), creditCard, date.Year, date.Month))
        {
            throw new ArgumentException(PaidStatementMessage);
        }
    }

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private (Category Category, Account? PaymentSourceAccount, Account? CreditCard) ValidateFields(
        string description, Guid categoryId, Guid? paymentSourceBankId, Guid? creditCardId)
    {
        DescriptionValidator.EnsureWithinLimit(description);

        var category = CashFlowReferenceResolver.ResolveActiveCategory(_repository.GetCategories(), categoryId);

        var parsedPaymentSourceBank = paymentSourceBankId is { } bankId
            ? CashFlowReferenceResolver.ResolvePaymentSource(_repository.GetAccounts(), bankId)
            : null;

        Account? parsedCreditCard = null;
        if (creditCardId is not null)
        {
            if (!EntityIdResolver.TryResolve(creditCardId, _repository.GetCreditCardAccounts(), c => c.Id, out var creditCard))
            {
                throw new ArgumentException($"Credit card '{creditCardId}' is not recognized.");
            }

            parsedCreditCard = creditCard;
        }

        return (category, parsedPaymentSourceBank, parsedCreditCard);
    }

    internal static ExpenseDTO ToDto(Expense expense) => new()
    {
        Id = expense.Id,
        Date = expense.Date,
        Description = expense.Description,
        Value = expense.Value,
        CategoryId = expense.Category.Id,
        CategoryName = expense.Category.Name,
        PaymentSourceBankId = expense.PaymentSourceAccount?.Id,
        PaymentSourceBankName = expense.PaymentSourceAccount?.Name,
        CreditCardId = expense.CreditCard?.Id,
        CreditCardName = expense.CreditCard?.Name,
        ChargeDate = expense.ChargeDate,
        InvoiceDate = expense.InvoiceDate,
        PaymentStatus = expense.PaymentStatus.ToString(),
        RoundUpAmount = expense.RoundUpAmount,
        SuggestedRoundUpAmount = expense.SuggestedRoundUpAmount,
        CountsAsTithe = expense.CountsAsTithe
    };
}
