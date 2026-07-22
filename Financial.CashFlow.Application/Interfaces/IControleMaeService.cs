using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IControleMaeService
{
    Task<MaeLedgerEntryDTO> CreateEntryAsync(CreateMaeLedgerEntryDTO request);
    IReadOnlyList<MaeLedgerEntryDTO> GetEntriesByMonth(int year, int month);
    Task<MaeLedgerEntryDTO> UpdateEntryValuesAsync(Guid id, UpdateMaeLedgerEntryValuesDTO request);
}
