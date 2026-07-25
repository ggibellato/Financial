using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.PaymentState;

/// <summary>
/// Outcome of one migration run: how many expenses ended in each payment state,
/// plus the card-tagged expenses that had no matching statement and need manual review.
/// </summary>
public sealed class PaymentStateMigrationSummary
{
    private readonly List<Expense> _missingStatementExpenses = new();

    public int ImmediatePaymentCount { get; private set; }
    public int NewlySettledCount { get; private set; }
    public int AlreadySettledCount { get; private set; }
    public int NewlyChargeCount { get; private set; }
    public int AlreadyChargeCount { get; private set; }

    public IReadOnlyList<Expense> MissingStatementExpenses => _missingStatementExpenses;

    public void CountImmediatePayment() => ImmediatePaymentCount++;
    public void CountNewlySettled() => NewlySettledCount++;
    public void CountAlreadySettled() => AlreadySettledCount++;
    public void CountNewlyCharge() => NewlyChargeCount++;
    public void CountAlreadyCharge() => AlreadyChargeCount++;

    public void FlagMissingStatement(Expense expense) => _missingStatementExpenses.Add(expense);

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Payment state migration summary");
        builder.AppendLine($"  ImmediatePayment (untouched): {ImmediatePaymentCount}");
        builder.AppendLine($"  CreditCardSettled: {NewlySettledCount} reclassified, {AlreadySettledCount} already conforming");
        builder.AppendLine($"  CreditCardCharge: {NewlyChargeCount} reclassified, {AlreadyChargeCount} already conforming");

        if (_missingStatementExpenses.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Card-tagged expenses with no matching statement (treated as unpaid, review manually):");
            foreach (var expense in _missingStatementExpenses)
            {
                builder.AppendLine($"  {expense.Id} {expense.Date:yyyy-MM-dd} '{expense.Description}' [{expense.CardTag}]");
            }
        }

        return builder.ToString();
    }
}
