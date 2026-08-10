using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Services;

public sealed class CardStatementService : ICardStatementService
{
    private static readonly Domain.Enums.CreditCard[] AllCards = Enum.GetValues<Domain.Enums.CreditCard>();

    private readonly ICashFlowRepository _repository;

    public CardStatementService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<CardStatementDTO>> GetStatementsForMonthAsync(int year, int month)
    {
        var existingStatements = _repository.GetCardStatements()
            .Where(s => s.Year == year && s.Month == month)
            .ToList();

        var created = false;
        foreach (var card in AllCards)
        {
            if (existingStatements.Any(s => s.Card == card))
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

        return existingStatements.Select(s => ToDto(s)).ToList();
    }

    public async Task<CardStatementDTO> MarkStatementPaidAsync(Guid id, MarkStatementPaidDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var statement = FindStatementOrThrow(id);

        if (statement.IsPaid)
        {
            return ToDto(statement);
        }

        if (!BankNameResolver.TryResolve(request.PaymentSourceBankId, _repository.GetBanks(), out var bank))
        {
            throw new ArgumentException($"Payment source '{request.PaymentSourceBankId}' is not recognized.");
        }

        var settledAt = DateOnly.FromDateTime(DateTime.Today);
        var charges = GetStatementExpenses(statement, ExpensePaymentStatus.CreditCardCharge);
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

        return ToDto(statement, warning);
    }

    public async Task<CardStatementDTO> UnmarkStatementPaidAsync(Guid id)
    {
        var statement = FindStatementOrThrow(id);

        if (!statement.IsPaid)
        {
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

        return ToDto(statement);
    }

    private CardStatement FindStatementOrThrow(Guid id) =>
        _repository.GetCardStatements().FirstOrDefault(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Card statement '{id}' was not found.");

    private List<Expense> GetStatementExpenses(CardStatement statement, ExpensePaymentStatus status) =>
        _repository.GetExpenses()
            .Where(e => e.CardTag == statement.Card
                && e.InvoiceDate is not null
                && e.InvoiceDate.Value.Year == statement.Year
                && e.InvoiceDate.Value.Month == statement.Month
                && e.PaymentStatus == status)
            .ToList();

    private CardStatementDTO ToDto(CardStatement statement, string? warning = null) => new()
    {
        Id = statement.Id,
        Card = statement.Card.ToString(),
        Year = statement.Year,
        Month = statement.Month,
        IsPaid = statement.IsPaid,
        OutstandingTotal = GetStatementExpenses(statement, ExpensePaymentStatus.CreditCardCharge).Sum(e => e.Value),
        Warning = warning
    };
}
