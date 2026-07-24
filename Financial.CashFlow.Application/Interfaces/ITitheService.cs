using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ITitheService
{
    TitheSummaryDTO GetTitheSummary(int year, int month);
}
