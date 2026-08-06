using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IIncomeSourceService
{
    IReadOnlyList<IncomeSourceDTO> GetIncomeSources();
}
