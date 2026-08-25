using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

/// <summary>
/// Deliberately exempt from the standard span/log wrapper (docs/rules/implementation.md "Public
/// service methods must make failure observable"): this is a pure, deterministic pass-through to
/// <see cref="Financial.Investment.Domain.Rules.ProfitCalculator"/>, which guards every division
/// and never throws for any input, and is called per-row in a hot rendering path
/// (<c>PortfolioAssetSummaryRowViewModel</c>) where span/logger overhead would add cost with
/// nothing to observe. Unlike <see cref="XirrCalculationService"/>, this one has no HTTP entry
/// point at all — it is only ever called from <c>Financial.App</c>.
/// </summary>
public sealed class ProfitCalculationService : IProfitCalculationService
{
    public bool HasCostBasis(decimal averagePrice, decimal quantity) =>
        ProfitCalculator.HasCostBasis(averagePrice, quantity);

    public decimal CalculateResultFraction(decimal averagePrice, decimal quantity, decimal currentValue) =>
        ProfitCalculator.CalculateResultFraction(averagePrice, quantity, currentValue);

    public decimal? CalculateProfitPercent(decimal currentValue, decimal costBasis) =>
        ProfitCalculator.CalculateProfitPercent(currentValue, costBasis);
}
