using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public sealed class FakeExchangeRateProvider : IExchangeRateProvider
{
    private readonly Func<DateOnly, Currency, Currency, decimal?> _resolver;

    public FakeExchangeRateProvider(Func<DateOnly, Currency, Currency, decimal?> resolver)
    {
        _resolver = resolver;
    }

    public int CallCount { get; private set; }

    public Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
    {
        CallCount++;
        return Task.FromResult(_resolver(date, from, to));
    }
}
