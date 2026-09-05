using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// One row of the merged Credit Card tab grid: a credit card plus its current month's statement,
/// if one exists. A deactivated card stops getting new monthly statements (see
/// CardStatementService), so <see cref="Statement"/> is null for it once the current month rolls
/// past its deactivation - the row still renders so the card stays manageable (e.g. re-activating
/// it), just with no Outstanding/Status/Mark Paid content.
/// </summary>
public sealed class CreditCardManagementRow
{
    public required CreditCardDTO CreditCard { get; init; }
    public CardStatementDTO? Statement { get; init; }

    public string CreditCardName => CreditCard.Name;
    public bool HasStatement => Statement is not null;
    public decimal OutstandingTotal => Statement?.OutstandingTotal ?? 0m;
    public decimal AccumulatedOutstandingTotal => Statement?.AccumulatedOutstandingTotal ?? 0m;
    public bool IsPaid => Statement?.IsPaid ?? false;
}
