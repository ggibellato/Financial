using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions;
using CreditCardEntity = Financial.CashFlow.Domain.Entities.CreditCard;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class ExpenseService : IExpenseService
{
    private const string EntityType = "Expense";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<ExpenseService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExpenseDTO> AddExpenseAsync(ExpenseCreateDTO request)
    {
        using var span = StartSpan("AddExpense");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var (category, paymentSource, creditCard) = ValidateFields(
                request.Description, request.Value, request.CategoryId, request.PaymentSourceBankId, request.CreditCardId);
            ValidateRoundUpEligibility(request.RoundUpAmount, paymentSource);

            var expense = Expense.Create(request.Date, request.Description, request.Value, category, paymentSource, creditCard, request.InvoiceDate, request.CountsAsTithe);
            expense.SetRoundUpAmount(request.RoundUpAmount);
            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddExpense(expense);
                return true;
            }).ConfigureAwait(false);

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
                request.Description, request.Value, request.CategoryId, request.PaymentSourceBankId, request.CreditCardId);
            ValidateRoundUpEligibility(request.RoundUpAmount, paymentSource);

            await _repository.ApplyAndSaveAsync(() =>
            {
                expense.UpdateDetails(request.Date, request.Description, request.Value, category, paymentSource, creditCard, request.CountsAsTithe);
                expense.SetRoundUpAmount(request.RoundUpAmount);

                if (request.InvoiceDate is not null && request.InvoiceDate != expense.InvoiceDate)
                {
                    expense.SetInvoiceDate(request.InvoiceDate.Value);
                }

                return true;
            }).ConfigureAwait(false);

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
            FindExpenseOrThrow(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteExpense(id);
                return true;
            }).ConfigureAwait(false);

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
                .Where(e => ListGroupingDate(e).Year == year && ListGroupingDate(e).Month == month)
                .Where(e => e.PaymentStatus != ExpensePaymentStatus.CreditCardCharge)
                .OrderByDescending(ListGroupingDate)
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
                .Where(e => ListGroupingDate(e).Year == year && ListGroupingDate(e).Month == month)
                .Where(e => e.PaymentStatus == ExpensePaymentStatus.CreditCardCharge)
                // Every unpaid charge in a given month shares the same InvoiceDate (the 1st of that
                // month), so ordering by ListGroupingDate here is a no-op tie that leaves rows in
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

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(ExpenseService), operationName, EntityType);
    }

    /// <summary>
    /// The month/year an expense is grouped and sorted under in the Expense/Card tab lists: a
    /// credit card expense (paid or unpaid) always uses its assigned <see cref="Expense.InvoiceDate"/>,
    /// which never changes across Settle()/Unsettle(), so an expense never moves to a different
    /// month's view purely because it was marked paid. Bank expenses fall back to <see cref="Expense.Date"/>.
    /// </summary>
    private static DateOnly ListGroupingDate(Expense expense) => expense.InvoiceDate ?? expense.ChargeDate ?? expense.Date;

    private Expense FindExpenseOrThrow(Guid id) =>
        _repository.GetExpenses().FirstOrThrow(e => e.Id == id, "Expense", id);

    private (Category Category, Bank? PaymentSourceBank, CreditCardEntity? CreditCard) ValidateFields(
        string description, decimal value, Guid categoryId, Guid? paymentSourceBankId, Guid? creditCardId)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.");
        }

        DescriptionValidator.EnsureWithinLimit(description);

        if (value == 0)
        {
            throw new ArgumentException("Value must not be zero.");
        }

        if (!EntityIdResolver.TryResolve(categoryId, _repository.GetCategories(), c => c.Id, out var category))
        {
            throw new ArgumentException($"Category '{categoryId}' is not recognized.");
        }

        if (!category!.Active)
        {
            throw new ArgumentException($"Category '{category.Name}' is inactive and cannot be used for new entries.");
        }

        Bank? parsedPaymentSourceBank = null;
        if (paymentSourceBankId is not null)
        {
            if (!EntityIdResolver.TryResolve(paymentSourceBankId, _repository.GetBanks(), b => b.Id, out var bank))
            {
                throw new ArgumentException($"Payment source '{paymentSourceBankId}' is not recognized.");
            }

            parsedPaymentSourceBank = bank!;
        }

        CreditCardEntity? parsedCreditCard = null;
        if (creditCardId is not null)
        {
            if (!EntityIdResolver.TryResolve(creditCardId, _repository.GetCreditCards(), c => c.Id, out var creditCard))
            {
                throw new ArgumentException($"Credit card '{creditCardId}' is not recognized.");
            }

            if (!creditCard!.IsActive)
            {
                throw new ArgumentException(
                    $"Credit card '{creditCard.Name}' is inactive and cannot be used for new entries.");
            }

            parsedCreditCard = creditCard;
        }

        return (category, parsedPaymentSourceBank, parsedCreditCard);
    }

    private static void ValidateRoundUpEligibility(decimal? roundUpAmount, Bank? paymentSourceBank)
    {
        if (roundUpAmount is null || paymentSourceBank is null)
        {
            return;
        }

        if (!paymentSourceBank.RoundUpEnabled)
        {
            throw new ArgumentException($"Bank '{paymentSourceBank.Name}' does not support round-up.");
        }
    }

    private static ExpenseDTO ToDto(Expense expense) => new()
    {
        Id = expense.Id,
        Date = expense.Date,
        Description = expense.Description,
        Value = expense.Value,
        CategoryId = expense.Category.Id,
        CategoryName = expense.Category.Name,
        PaymentSourceBankId = expense.PaymentSourceBank?.Id,
        PaymentSourceBankName = expense.PaymentSourceBank?.Name,
        CreditCardId = expense.CreditCard?.Id,
        CreditCardName = expense.CreditCard?.Name,
        ChargeDate = expense.ChargeDate,
        InvoiceDate = expense.InvoiceDate,
        PaymentStatus = expense.PaymentStatus.ToString(),
        RoundUpAmount = expense.RoundUpAmount,
        SuggestedRoundUpAmount = GetSuggestedRoundUpAmount(expense),
        CountsAsTithe = expense.CountsAsTithe
    };

    private static decimal? GetSuggestedRoundUpAmount(Expense expense)
    {
        if (expense.RoundUpAmount is not null
            || expense.PaymentStatus != ExpensePaymentStatus.ImmediatePayment
            || expense.Value <= 0)
        {
            return null;
        }

        return expense.PaymentSourceBank?.RoundUpEnabled == true
            ? expense.RoundUpSuggestion
            : null;
    }
}
