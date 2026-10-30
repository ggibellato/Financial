using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IAccountGroupSummaryService
{
    AccountGroupSummaryDTO GetDefaultGroupSummary(int year, int month);

    IReadOnlyList<AccountTemplateGroupDTO> GetTemplateGroups(int year, int month);
}
