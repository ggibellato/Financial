namespace Financial.Shared.Abstractions.Currencies.FxRates;

public interface IUsdRateFetcher
{
    Task<UsdRateFetchResult> FetchAsync(DateOnly date);
}
