using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Application.Validation;

internal static class PotWithdrawalValidator
{
    private const string PaidStatementMessage = "This statement is already paid; choose a later month.";
    private const string StatementWithoutCardMessage = "A statement month is only used when paying by card.";

    internal static PotWithdrawalPayment ResolvePayment(
        PotWithdrawalRequestDTO request,
        IReadOnlyList<Account> accounts,
        IEnumerable<Account> creditCards,
        IEnumerable<Category> categories,
        IEnumerable<CardStatement> statements)
    {
        var card = request.CreditCardId is { } cardId ? ResolveCard(request, creditCards, cardId, statements) : null;
        if (card is null && (request.StatementYear is not null || request.StatementMonth is not null))
        {
            throw new ArgumentException(StatementWithoutCardMessage);
        }

        if (request.BankId is null && card is null)
        {
            return request.CategoryId is null
                ? new(null, null, null)
                : throw new ArgumentException("A bank or credit card is required when an expense category is given.");
        }

        var bank = request.BankId is { } bankId ? CashFlowReferenceResolver.ResolvePaymentSource(accounts, bankId) : null;
        return new(bank, card, ResolveCategory(request.CategoryId, request.Description, accounts, categories));
    }

    internal static void EnsureWithinBalance(Pot pot, decimal amount, bool confirmed, IEnumerable<PotTransaction> movements)
    {
        var balance = AccountBalanceRule.PotBalance(pot, DateOnly.MaxValue, movements);
        if (amount > balance && !confirmed)
        {
            throw new OverdraftConfirmationRequiredException(pot.Name, balance);
        }
    }

    internal static DateOnly? RequestedStatementMonth(PotWithdrawalRequestDTO request) =>
        StatementMonthParser.ParseOptional(request.StatementYear, request.StatementMonth);

    private static PotWithdrawalCard ResolveCard(
        PotWithdrawalRequestDTO request, IEnumerable<Account> creditCards, Guid cardId, IEnumerable<CardStatement> statements)
    {
        var card = CashFlowReferenceResolver.ResolveCreditCard(creditCards, cardId);
        var statementMonth = RequestedStatementMonth(request) ?? new DateOnly(request.Date.Year, request.Date.Month, 1);
        if (CardChargeFilter.IsStatementPaid(statements, card, statementMonth.Year, statementMonth.Month))
        {
            throw new ArgumentException(PaidStatementMessage);
        }

        return new(card, statementMonth);
    }

    internal static Category? ResolveCategory(Guid? categoryId, string description, IEnumerable<Account> accounts, IEnumerable<Category> categories)
    {
        if (categoryId is null)
        {
            return null;
        }

        DescriptionValidator.EnsureWithinLimit(description);
        var category = CashFlowReferenceResolver.ResolveActiveCategory(categories, categoryId.Value);
        PotWithdrawal.EnsureUsableCategory(category, accounts);
        return category;
    }
}
