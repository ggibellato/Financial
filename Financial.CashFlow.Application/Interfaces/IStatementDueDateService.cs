using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IStatementDueDateService
{
    Task SetDueDateAsync(Guid accountId, int year, int month, StatementDueDateUpdateDTO request);
}
