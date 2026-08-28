using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IControleMaeService
{
    Task<MaeLedgerEntryDTO> CreateEntryAsync(MaeLedgerEntryCreateDTO request);
    IReadOnlyList<MaeLedgerEntryDTO> GetEntriesFromDate(DateOnly fromDate);
    MaeLedgerTotalsDTO GetTotals();
    Task<MaeLedgerEntryDTO> UpdateEntryValuesAsync(Guid id, MaeLedgerEntryValuesUpdateDTO request);
    Task DeleteEntryAsync(Guid id);
}
