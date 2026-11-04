using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Application.Validation;

internal static class PotWithdrawalValidator
{
    internal static (Account? Bank, Category? Category) ResolveBankAndCategory(
        Guid? bankId, Guid? categoryId, string description, IReadOnlyList<Account> accounts, IEnumerable<Category> categories)
    {
        if (bankId is null && categoryId is null)
        {
            return (null, null);
        }

        if (bankId is null)
        {
            throw new ArgumentException("A bank is required when an expense category is given.");
        }

        if (categoryId is null)
        {
            throw new ArgumentException("Category is required when a bank is selected.");
        }

        DescriptionValidator.EnsureWithinLimit(description);

        var bank = CashFlowReferenceResolver.ResolvePaymentSource(accounts, bankId.Value);
        var category = CashFlowReferenceResolver.ResolveActiveCategory(categories, categoryId.Value);
        PotWithdrawal.EnsureUsableCategory(category, accounts);

        return (bank, category);
    }

    internal static void EnsureWithinBalance(Pot pot, decimal amount, bool confirmed, IEnumerable<PotMovement> movements)
    {
        var balance = AccountBalanceRule.PotBalance(pot, DateOnly.MaxValue, movements);
        if (amount > balance && !confirmed)
        {
            throw new OverdraftConfirmationRequiredException(
                FormattableString.Invariant($"This withdrawal exceeds {pot.Name}'s balance of {balance:F2}. Set confirmed=true to proceed."));
        }
    }
}
