using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICardStatementService
{
    Task<IReadOnlyList<CardStatementDTO>> GetStatementsForMonthAsync(int year, int month);
    Task<CardStatementDTO> MarkStatementPaidAsync(Guid id, MarkCardStatementPaidDTO request);
    Task<CardStatementDTO> UnmarkStatementPaidAsync(Guid id);

    /// <summary>Sums a credit card's <see cref="Financial.CashFlow.Domain.Enums.ExpensePaymentStatus.CreditCardCharge"/>
    /// expenses for the given invoice year/month directly - works whether or not a
    /// <see cref="Financial.CashFlow.Domain.Entities.CardStatement"/> row exists yet for that
    /// period. <c>HasChargesPosted</c> is <see langword="false"/> when no expenses matched, which
    /// is a distinct fact from the total happening to be zero.</summary>
    (decimal Total, bool HasChargesPosted) GetOutstandingTotalForPeriod(Guid creditCardId, int year, int month);
}
