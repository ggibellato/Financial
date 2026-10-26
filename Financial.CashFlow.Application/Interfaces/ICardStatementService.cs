using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICardStatementService
{
    IReadOnlyList<CardStatementDTO> GetStatementsForMonth(int year, int month);
    Task<CardStatementDTO> MarkStatementPaidAsync(Guid id, MarkCardStatementPaidDTO request);
    Task<CardStatementDTO> UnmarkStatementPaidAsync(Guid id);

    (decimal Total, bool HasChargesPosted) GetOutstandingTotalForPeriod(Guid creditCardId, int year, int month);
}
