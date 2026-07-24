using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowPaymentStateMigration;

/// <summary>
/// Reclassifies every stored <see cref="Expense"/> into the F01 payment-state model using only
/// the entity's own Settle/Unsettle transitions, so the result can never violate the domain
/// invariant. Re-runnable: records already in a conforming shape are left untouched, including
/// settled expenses whose real settlement date was recorded by the statement settlement flow.
/// </summary>
public static class ExpensePaymentStateMigrator
{
    public static MigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new MigrationSummary();
        var paidStatements = data.CardStatements
            .Where(s => s.IsPaid)
            .ToDictionary(s => (s.Card, s.Year, s.Month));
        var allStatements = data.CardStatements
            .Select(s => (s.Card, s.Year, s.Month))
            .ToHashSet();

        foreach (var expense in data.Expenses)
        {
            MigrateExpense(expense, paidStatements, allStatements, summary);
        }

        return summary;
    }

    private static void MigrateExpense(
        Expense expense,
        IReadOnlyDictionary<(CreditCard, int, int), CardStatement> paidStatements,
        IReadOnlySet<(CreditCard, int, int)> allStatements,
        MigrationSummary summary)
    {
        if (expense.CardTag is null)
        {
            summary.CountImmediatePayment();
            return;
        }

        var statementKey = (expense.CardTag.Value, expense.Date.Year, expense.Date.Month);
        if (!allStatements.Contains(statementKey))
        {
            summary.FlagMissingStatement(expense);
            ReclassifyAsCharge(expense, summary);
            return;
        }

        if (paidStatements.ContainsKey(statementKey))
        {
            ReclassifyAsSettled(expense, summary);
            return;
        }

        ReclassifyAsCharge(expense, summary);
    }

    private static void ReclassifyAsSettled(Expense expense, MigrationSummary summary)
    {
        if (expense.PaymentSource is null)
        {
            // Already a charge; the settling bank was never recorded, so there is nothing to infer.
            summary.CountAlreadyCharge();
            return;
        }

        if (expense.SettledAt is not null)
        {
            summary.CountAlreadySettled();
            return;
        }

        var settlingBank = expense.PaymentSource!;
        expense.Unsettle();
        expense.Settle(settlingBank, LastDayOfMonth(expense.Date));
        summary.CountNewlySettled();
    }

    private static void ReclassifyAsCharge(Expense expense, MigrationSummary summary)
    {
        if (expense.PaymentSource is null)
        {
            summary.CountAlreadyCharge();
            return;
        }

        expense.Unsettle();
        summary.CountNewlyCharge();
    }

    private static DateOnly LastDayOfMonth(DateOnly date) =>
        new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
}
