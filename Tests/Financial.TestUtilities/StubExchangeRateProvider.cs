using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public sealed class StubExchangeRateProvider : IExchangeRateProvider
{
    private readonly decimal? _rate;

    public StubExchangeRateProvider(decimal? rate)
    {
        _rate = rate;
    }

    public int CallCount { get; private set; }

    public Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
    {
        CallCount++;
        return Task.FromResult(_rate);
    }
}
