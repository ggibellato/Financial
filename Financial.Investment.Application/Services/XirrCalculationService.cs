using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

public sealed class XirrCalculationService : IXirrCalculationService
{
    public decimal? Calculate(IReadOnlyList<AssetCashFlowDTO> cashFlows, decimal terminalValue)
    {
        var series = new List<(DateTime Date, decimal Amount)>(cashFlows.Count + 1);
        series.AddRange(cashFlows.Select(cf => (cf.Date, cf.Amount)));
        series.Add((DateTime.Today, terminalValue));

        return XirrCalculator.Calculate(series);
    }
}
