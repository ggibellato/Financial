using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface IDividendService
{
    IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request);
    DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request);
}
