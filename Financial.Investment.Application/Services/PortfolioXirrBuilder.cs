using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

internal readonly record struct PortfolioXirrResult(decimal? GrossXirr, decimal? NetXirr);

/// <summary>
/// The portfolio series carries one terminal flow per priced Active holding, a shape
/// IXirrCalculationService's single-terminal-value signature cannot express. A closed Historic
/// position contributes its dated flows only, keeping the existing zero-terminal convention.
/// </summary>
internal static class PortfolioXirrBuilder
{
    public static PortfolioXirrResult Calculate(IReadOnlyList<PortfolioHolding> holdings, DateTime asOf) =>
        new(
            XirrCalculator.Calculate(BuildSeries(holdings, asOf, AssetCashFlowBuilder.BuildWithCredits)),
            XirrCalculator.Calculate(BuildSeries(holdings, asOf, AssetCashFlowBuilder.BuildNetOfTaxWithCredits)));

    public static IReadOnlyList<(DateTime Date, decimal Amount)> BuildSeries(
        IReadOnlyList<PortfolioHolding> holdings,
        DateTime asOf,
        Func<Asset, IReadOnlyList<AssetCashFlowDTO>> datedFlows)
    {
        var series = new List<(DateTime Date, decimal Amount)>();

        foreach (var holding in holdings)
        {
            foreach (var flow in datedFlows(holding.Asset))
            {
                series.Add((flow.Date, flow.Amount));
            }

            if (holding.HasTerminalValue)
            {
                series.Add((asOf, holding.MarketValue!.Value));
            }
        }

        return series;
    }
}
