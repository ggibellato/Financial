using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IAccountAdminService
{
    IReadOnlyList<AccountTemplateDTO> GetTemplates();
    Task<AccountDTO> CreateAsync(AccountCreateDTO request);
    Task<AccountDTO> UpdateAsync(Guid id, AccountUpdateDTO request);
    Task DeleteAsync(Guid id);
}
