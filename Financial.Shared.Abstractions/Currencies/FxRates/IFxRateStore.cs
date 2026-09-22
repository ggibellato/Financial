using Financial.Shared.Abstractions.Sync;

namespace Financial.Shared.Abstractions.Currencies.FxRates;

public interface IFxRateStore : ISyncStatusProvider
{
    FxRateRecord? TryGetRate(DateOnly date);

    Task SetRateAsync(DateOnly date, FxRateRecord record);
}
