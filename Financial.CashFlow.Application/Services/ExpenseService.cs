using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Services;

public sealed class ExpenseService : IExpenseService
{
    private const int MaxDescriptionLength = 200;

    private readonly ICashFlowRepository _repository;

    public ExpenseService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ExpenseDTO> AddExpenseAsync(ExpenseCreateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (category, paymentSource, cardTag) = ValidateFields(
            request.Description, request.Value, request.Category, request.PaymentSource, request.CardTag);
        ValidateRoundUpEligibility(request.RoundUpAmount, paymentSource);

        var expense = Expense.Create(request.Date, request.Description, request.Value, category, paymentSource, cardTag, request.InvoiceDate);
        expense.SetRoundUpAmount(request.RoundUpAmount);
        _repository.AddExpense(expense);
        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(expense);
    }

    public async Task<ExpenseDTO> UpdateExpenseAsync(Guid id, ExpenseUpdateDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var expense = FindExpenseOrThrow(id);

        var (category, paymentSource, cardTag) = ValidateFields(
            request.Description, request.Value, request.Category, request.PaymentSource, request.CardTag);
        ValidateRoundUpEligibility(request.RoundUpAmount, paymentSource);

        expense.UpdateDetails(request.Date, request.Description, request.Value, category, paymentSource, cardTag);
        expense.SetRoundUpAmount(request.RoundUpAmount);

        if (request.InvoiceDate is not null && request.InvoiceDate != expense.InvoiceDate)
        {
            expense.SetInvoiceDate(request.InvoiceDate.Value);
        }

        await _repository.SaveChangesAsync().ConfigureAwait(false);

        return ToDto(expense);
    }

    public async Task DeleteExpenseAsync(Guid id)
    {
        FindExpenseOrThrow(id);

        _repository.DeleteExpense(id);
        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }

    public IReadOnlyList<ExpenseDTO> GetExpensesByMonth(int year, int month) =>
        _repository.GetExpenses()
            .Where(e => ListGroupingDate(e).Year == year && ListGroupingDate(e).Month == month)
            .Where(e => e.PaymentStatus != ExpensePaymentStatus.CreditCardCharge)
            .OrderByDescending(ListGroupingDate)
            .Select(ToDto)
            .ToList();

    public IReadOnlyList<ExpenseDTO> GetUnpaidCardChargesByMonth(int year, int month) =>
        _repository.GetExpenses()
            .Where(e => ListGroupingDate(e).Year == year && ListGroupingDate(e).Month == month)
            .Where(e => e.PaymentStatus == ExpensePaymentStatus.CreditCardCharge)
            .OrderByDescending(ListGroupingDate)
            .Select(ToDto)
            .ToList();

    public IReadOnlyList<CategoryTotalDTO> GetCategoryTotalsByMonth(int year, int month) =>
        _repository.GetExpenses()
            .Where(e => CategoryTotalDate(e).Year == year && CategoryTotalDate(e).Month == month)
            .GroupBy(e => e.Category)
            .Select(g => new CategoryTotalDTO
            {
                Category = g.Key.ToString(),
                TotalValue = g.Sum(e => e.Value)
            })
            .ToList();

    /// <summary>
    /// The month/year an expense is grouped and sorted under in the Expense/Card tab lists: a
    /// credit card expense (paid or unpaid) always uses its assigned <see cref="Expense.InvoiceDate"/>,
    /// which never changes across Settle()/Unsettle(), so an expense never moves to a different
    /// month's view purely because it was marked paid. Bank expenses fall back to <see cref="Expense.Date"/>.
    /// </summary>
    private static DateOnly ListGroupingDate(Expense expense) => expense.InvoiceDate ?? expense.ChargeDate ?? expense.Date;

    /// <summary>
    /// The month/year an expense's value counts toward in category-total reporting: an unpaid
    /// credit card charge counts toward its assigned invoice period, while a settled charge or a
    /// bank expense counts toward its (post-settlement, for a charge) <see cref="Expense.Date"/>.
    /// </summary>
    private static DateOnly CategoryTotalDate(Expense expense) =>
        expense.PaymentStatus == ExpensePaymentStatus.CreditCardCharge ? expense.InvoiceDate!.Value : expense.Date;

    private Expense FindExpenseOrThrow(Guid id) =>
        _repository.GetExpenses().FirstOrDefault(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Expense '{id}' was not found.");

    private (Category Category, string? PaymentSource, CreditCard? CardTag) ValidateFields(
        string description, decimal value, string category, string? paymentSource, string? cardTag)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.");
        }

        if (description.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Description must not exceed {MaxDescriptionLength} characters.");
        }

        if (value == 0)
        {
            throw new ArgumentException("Value must not be zero.");
        }

        if (!CategoryParser.TryParse(category, out var parsedCategory))
        {
            throw new ArgumentException($"Category '{category}' is not recognized.");
        }

        string? parsedPaymentSource = null;
        if (!string.IsNullOrWhiteSpace(paymentSource))
        {
            if (!BankNameResolver.TryResolve(paymentSource, _repository.GetBanks(), out var bank))
            {
                throw new ArgumentException($"Payment source '{paymentSource}' is not recognized.");
            }

            parsedPaymentSource = bank!.Name;
        }

        CreditCard? parsedCardTag = null;
        if (!string.IsNullOrWhiteSpace(cardTag))
        {
            if (!CreditCardParser.TryParse(cardTag, out var creditCard))
            {
                throw new ArgumentException($"Credit card '{cardTag}' is not recognized.");
            }

            parsedCardTag = creditCard;
        }

        return (parsedCategory, parsedPaymentSource, parsedCardTag);
    }

    private void ValidateRoundUpEligibility(decimal? roundUpAmount, string? paymentSource)
    {
        if (roundUpAmount is null || paymentSource is null)
        {
            return;
        }

        if (BankNameResolver.TryResolve(paymentSource, _repository.GetBanks(), out var bank) && !bank!.RoundUpEnabled)
        {
            throw new ArgumentException($"Bank '{bank.Name}' does not support round-up.");
        }
    }

    private ExpenseDTO ToDto(Expense expense) => new()
    {
        Id = expense.Id,
        Date = expense.Date,
        Description = expense.Description,
        Value = expense.Value,
        Category = expense.Category.ToString(),
        PaymentSource = expense.PaymentSource,
        CardTag = expense.CardTag?.ToString(),
        ChargeDate = expense.ChargeDate,
        InvoiceDate = expense.InvoiceDate,
        PaymentStatus = expense.PaymentStatus.ToString(),
        RoundUpAmount = expense.RoundUpAmount,
        SuggestedRoundUpAmount = GetSuggestedRoundUpAmount(expense)
    };

    private decimal? GetSuggestedRoundUpAmount(Expense expense)
    {
        if (expense.RoundUpAmount is not null
            || expense.PaymentStatus != ExpensePaymentStatus.ImmediatePayment
            || expense.Value <= 0)
        {
            return null;
        }

        return BankNameResolver.TryResolve(expense.PaymentSource, _repository.GetBanks(), out var bank) && bank!.RoundUpEnabled
            ? expense.RoundUpSuggestion
            : null;
    }
}
