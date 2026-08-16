using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ExpenseChargeDate;

/// <summary>
/// Backfills ChargeDate/InvoiceDate on every pre-existing credit card expense, using only the
/// entity's own MigrateLegacyDates method so the result can never violate the domain invariant.
/// Re-runnable: records that already have ChargeDate populated are left untouched.
/// </summary>
public static class ExpenseChargeDateMigrator
{
    public static ExpenseChargeDateMigrationSummary Migrate(CashFlowData data, string? legacyRawJson)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new ExpenseChargeDateMigrationSummary();
        var legacySettledAtById = LegacySettledAtExtractor.Extract(legacyRawJson);
        var paidStatementsByKey = data.CardStatements
            .Where(s => s.IsPaid)
            .ToDictionary(s => (s.CreditCard.Id, s.Year, s.Month));

        foreach (var expense in data.Expenses)
        {
            MigrateExpense(expense, legacySettledAtById, paidStatementsByKey, summary);
        }

        return summary;
    }

    private static void MigrateExpense(
        Expense expense,
        IReadOnlyDictionary<Guid, DateOnly> legacySettledAtById,
        IReadOnlyDictionary<(Guid, int, int), CardStatement> paidStatements,
        ExpenseChargeDateMigrationSummary summary)
    {
        if (expense.CreditCard is null)
        {
            summary.CountBankExpense();
            return;
        }

        if (expense.ChargeDate is not null)
        {
            summary.CountAlreadyMigrated();
            return;
        }

        if (expense.PaymentStatus == ExpensePaymentStatus.CreditCardCharge)
        {
            var invoiceDate = new DateOnly(expense.Date.Year, expense.Date.Month, 1);
            expense.MigrateLegacyDates(chargeDate: expense.Date, invoiceDate: invoiceDate, settledDate: null);
            summary.CountNewlyMigratedUnpaid();
            return;
        }

        var statementKey = (expense.CreditCard.Id, expense.Date.Year, expense.Date.Month);
        if (!paidStatements.TryGetValue(statementKey, out var statement))
        {
            summary.FlagMissingStatement(expense);
            return;
        }

        if (!legacySettledAtById.TryGetValue(expense.Id, out var settledAt))
        {
            summary.FlagMissingSettledAt(expense);
            return;
        }

        var settledInvoiceDate = new DateOnly(statement.Year, statement.Month, 1);
        expense.MigrateLegacyDates(chargeDate: expense.Date, invoiceDate: settledInvoiceDate, settledDate: settledAt);
        summary.CountNewlyMigratedSettled();
    }
}
