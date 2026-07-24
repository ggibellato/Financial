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

        var expense = Expense.Create(request.Date, request.Description, request.Value, category, paymentSource, cardTag);
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

        expense.UpdateDetails(request.Date, request.Description, request.Value, category, paymentSource, cardTag);
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
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .Select(ToDto)
            .ToList();

    public IReadOnlyList<CategoryTotalDTO> GetCategoryTotalsByMonth(int year, int month) =>
        _repository.GetExpenses()
            .Where(e => e.Date.Year == year && e.Date.Month == month)
            .GroupBy(e => e.Category)
            .Select(g => new CategoryTotalDTO
            {
                Category = g.Key.ToString(),
                TotalValue = g.Sum(e => e.Value)
            })
            .ToList();

    private Expense FindExpenseOrThrow(Guid id) =>
        _repository.GetExpenses().FirstOrDefault(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Expense '{id}' was not found.");

    private static (Category Category, PaymentSource? PaymentSource, CreditCard? CardTag) ValidateFields(
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

        PaymentSource? parsedPaymentSource = null;
        if (!string.IsNullOrWhiteSpace(paymentSource))
        {
            if (!PaymentSourceParser.TryParse(paymentSource, out var source))
            {
                throw new ArgumentException($"Payment source '{paymentSource}' is not recognized.");
            }

            parsedPaymentSource = source;
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

    private static ExpenseDTO ToDto(Expense expense) => new()
    {
        Id = expense.Id,
        Date = expense.Date,
        Description = expense.Description,
        Value = expense.Value,
        Category = expense.Category.ToString(),
        PaymentSource = expense.PaymentSource?.ToString(),
        CardTag = expense.CardTag?.ToString(),
        SettledAt = expense.SettledAt,
        PaymentStatus = expense.PaymentStatus.ToString()
    };
}
