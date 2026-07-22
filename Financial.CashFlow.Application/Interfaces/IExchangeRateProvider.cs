using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Interfaces;

public interface IExchangeRateProvider
{
    Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to);
}
