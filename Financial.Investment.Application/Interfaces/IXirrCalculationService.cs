using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface IXirrCalculationService
{
    decimal? Calculate(IReadOnlyList<AssetCashFlowDTO> cashFlows, decimal terminalValue);
    decimal? Calculate(IReadOnlyList<AssetCashFlowDTO> cashFlows, decimal terminalValue, DateTime asOf);
}
