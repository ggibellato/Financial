using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICardStatementService
{
    Task<IReadOnlyList<CardStatementDTO>> GetStatementsForMonthAsync(int year, int month);
    Task<CardStatementDTO> MarkStatementPaidAsync(Guid id);
}
