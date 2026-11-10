using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICardStatementService
{
    Task<CardStatementDTO> MarkStatementPaidAsync(Guid id, MarkCardStatementPaidDTO request);
    Task<CardStatementDTO> UnmarkStatementPaidAsync(Guid id);

    decimal? GetAmountDueForPeriod(Guid creditCardId, int year, int month);
}
