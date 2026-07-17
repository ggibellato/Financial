using Financial.Application.Interfaces;
using Financial.Domain.Rules;

namespace Financial.Application.Services;

public sealed class ProfitCalculationService : IProfitCalculationService
{
    public bool HasCostBasis(decimal averagePrice, decimal quantity) =>
        ProfitCalculator.HasCostBasis(averagePrice, quantity);

    public decimal CalculateResultFraction(decimal averagePrice, decimal quantity, decimal currentValue) =>
        ProfitCalculator.CalculateResultFraction(averagePrice, quantity, currentValue);

    public decimal? CalculateProfitPercent(decimal currentValue, decimal costBasis) =>
        ProfitCalculator.CalculateProfitPercent(currentValue, costBasis);
}
