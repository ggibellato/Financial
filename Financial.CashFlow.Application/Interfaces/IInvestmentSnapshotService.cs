using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IInvestmentSnapshotService
{
    Task<IReadOnlyList<InvestmentSnapshotDTO>> GetSnapshotsForMonthAsync(int year, int month);
    Task<InvestmentSnapshotDTO> UpdateSnapshotValueAsync(Guid id, InvestmentSnapshotValueUpdateDTO request);
    Task<InvestmentSnapshotSuggestionsDTO> GetSuggestionsForMonthAsync(int year, int month);
}
