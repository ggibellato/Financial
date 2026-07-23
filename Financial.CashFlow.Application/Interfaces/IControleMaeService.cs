using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IControleMaeService
{
    Task<MaeLedgerEntryDTO> CreateEntryAsync(CreateMaeLedgerEntryDTO request);
    IReadOnlyList<MaeLedgerEntryDTO> GetEntriesFromDate(DateOnly fromDate);
    MaeLedgerTotalsDTO GetTotals();
    Task<MaeLedgerEntryDTO> UpdateEntryValuesAsync(Guid id, UpdateMaeLedgerEntryValuesDTO request);
    Task DeleteEntryAsync(Guid id);
}
