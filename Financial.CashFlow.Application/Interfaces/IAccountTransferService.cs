using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IAccountTransferService
{
    Task<AccountTransferDTO> AddAsync(AccountTransferCreateDTO request);
    Task<AccountTransferDTO> UpdateAsync(Guid id, AccountTransferUpdateDTO request);
    Task DeleteAsync(Guid id);
    IReadOnlyList<TransferAccountOptionDTO> GetDestinations(Guid accountId);
    IReadOnlyList<TransferAccountOptionDTO> GetSources(Guid accountId);
}
