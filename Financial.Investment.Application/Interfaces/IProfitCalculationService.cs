namespace Financial.Investment.Application.Interfaces;

public interface IProfitCalculationService
{
    bool HasCostBasis(decimal averagePrice, decimal quantity);
    decimal CalculateResultFraction(decimal averagePrice, decimal quantity, decimal currentValue);
    decimal? CalculateProfitPercent(decimal currentValue, decimal costBasis);
}
