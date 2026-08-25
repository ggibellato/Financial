using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

/// <summary>
/// Deliberately exempt from the standard span/log wrapper (docs/rules/implementation.md "Public
/// service methods must make failure observable"): this is a pure, deterministic pass-through to
/// <see cref="Financial.Investment.Domain.Rules.XirrCalculator"/>, which is explicitly engineered
/// to never throw for any input, and is called per-row in a hot rendering path
/// (<c>PortfolioAssetSummaryRowViewModel</c>) where span/logger overhead would add cost with
/// nothing to observe.
/// </summary>
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
