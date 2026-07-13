using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IXirrCalculationService
{
    decimal? Calculate(IReadOnlyList<AssetCashFlowDTO> cashFlows, decimal terminalValue);
}
