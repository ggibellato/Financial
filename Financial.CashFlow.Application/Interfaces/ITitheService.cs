using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ITitheService
{
    Task<TitheSummaryDTO> GetTitheSummaryAsync(int year, int month);
    Task<TitheSummaryDTO> UpdateCarryForwardInclusionAsync(int year, int month, bool included);
}
