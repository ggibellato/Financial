using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IDividendService
{
    IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request);
    DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request);
}
