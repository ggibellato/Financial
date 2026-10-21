using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IAccountGroupSummaryService
{
    AccountGroupSummaryDTO GetDefaultGroupSummary(int year, int month);
}
