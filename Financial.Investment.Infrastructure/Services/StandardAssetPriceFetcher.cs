using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Services;

public sealed class StandardAssetPriceFetcher : IAssetPriceFetcher
{
    private readonly IFinanceService _financeService;

    public StandardAssetPriceFetcher(IFinanceService financeService)
    {
        _financeService = financeService ?? throw new ArgumentNullException(nameof(financeService));
    }

    /// <summary>
    /// The classes that are actually quoted on an exchange under a ticker. This used to be an
    /// exclusion list - everything except Cryptocurrency and Bond - which quietly claimed Cash,
    /// Pension, Other and PrivateCredit as well, and then asked the finance provider for a ticker
    /// that does not exist there. Unknown stays supported: it is the default for assets that were
    /// imported without a class, and most working holdings carry it.
    /// </summary>
    private static readonly HashSet<GlobalAssetClass> ExchangeListedClasses =
    [
        GlobalAssetClass.Unknown,
        GlobalAssetClass.Equity,
        GlobalAssetClass.RealEstate,
        GlobalAssetClass.Fund,
        GlobalAssetClass.ETF
    ];

    public bool Supports(GlobalAssetClass assetClass) => ExchangeListedClasses.Contains(assetClass);

    public AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Exchange))
        {
            throw new ArgumentException("Exchange is required.", nameof(request));
        }

        return _financeService.GetAssetValue(new AssetValueRequestDTO { Exchange = request.Exchange, Ticker = request.Ticker });
    }
}
