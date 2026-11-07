using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ISettlementService
{
    Task<SettlementEntryDTO> AddEntryAsync(Guid accountId, SettlementEntryCreateDTO request);
    Task<SettlementEntryDTO> UpdateEntryAsync(Guid accountId, Guid entryId, SettlementEntryUpdateDTO request);
    Task DeleteEntryAsync(Guid accountId, Guid entryId);
}
