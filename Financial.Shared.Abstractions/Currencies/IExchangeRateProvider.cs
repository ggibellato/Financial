namespace Financial.Shared.Abstractions.Currencies;

public interface IExchangeRateProvider
{
    Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to);
}
