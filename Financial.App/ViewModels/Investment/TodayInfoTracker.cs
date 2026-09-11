using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.ViewModels.Investment;

public sealed class TodayInfoTracker
{
    private readonly Action<TodayInfoSnapshot> _applySnapshot;
    private readonly Action _resetSnapshot;
    private readonly Action _updateCommandStates;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, TodayInfoSnapshot> _cache = new();
    private bool _attempted;
    private bool _isLoading;
    private string _assetKey = string.Empty;

    public TodayInfoTracker(Action<TodayInfoSnapshot> applySnapshot, Action resetSnapshot, Action updateCommandStates)
    {
        _applySnapshot = applySnapshot ?? throw new ArgumentNullException(nameof(applySnapshot));
        _resetSnapshot = resetSnapshot ?? throw new ArgumentNullException(nameof(resetSnapshot));
        _updateCommandStates = updateCommandStates ?? throw new ArgumentNullException(nameof(updateCommandStates));
    }

    public bool IsLoading => _isLoading;

    public bool CanRefresh(bool hasAssetContext) => hasAssetContext && !_isLoading;

    public void UpdateAssetKey(string assetKey)
    {
        if (string.Equals(_assetKey, assetKey, StringComparison.Ordinal))
        {
            return;
        }

        _assetKey = assetKey;
        _isLoading = false;

        if (_cache.TryGetValue(assetKey, out var cached))
        {
            _applySnapshot(cached);
            _attempted = true;
            return;
        }

        _attempted = false;
        _resetSnapshot();
    }

    public void Clear()
    {
        _assetKey = string.Empty;
        _attempted = false;
        _isLoading = false;
        _resetSnapshot();
    }

    /// <summary>A live quote carries a time of day; a price read from Price History carries only
    /// a date, so it is shown as one rather than padded with a midnight that was never measured.
    /// Falls back to an em dash, matching the web's <c>formatAsOf</c> in AssetSummaryTab.tsx.</summary>
    private static string FormatAsOf(AssetPriceDTO price) =>
        price.AsOf?.ToLocalTime().ToString("g")
        ?? price.AsOfDate?.ToString("d")
        ?? "—";

    /// <returns>True when a freshly fetched price was applied - false when the refresh was
    /// skipped, superseded by a newer asset selection, or failed.</returns>
    public async Task<bool> RefreshAsync(
        bool forceRefresh,
        bool hasAssetContext,
        IAssetPriceLookupService? priceService,
        GlobalAssetClass assetClass,
        string? brokerName,
        string exchange,
        string ticker,
        string? name,
        string? portfolioName,
        string? assetName,
        Action<string> setMessage)
    {
        if (!hasAssetContext)
        {
            setMessage("Select an asset to load current values.");
            return false;
        }

        if (priceService == null)
        {
            setMessage("Current value service is not available.");
            return false;
        }

        var isCryptocurrency = assetClass == GlobalAssetClass.Cryptocurrency;
        var isBond = assetClass == GlobalAssetClass.Bond;

        if (string.IsNullOrWhiteSpace(ticker))
        {
            setMessage("Asset exchange or ticker is missing.");
            return false;
        }

        if (isBond)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                setMessage("Asset name is missing.");
                return false;
            }
        }
        else if (!isCryptocurrency && string.IsNullOrWhiteSpace(exchange))
        {
            setMessage("Asset exchange or ticker is missing.");
            return false;
        }

        await _lock.WaitAsync();
        var assetKey = _assetKey;
        try
        {
            if (!forceRefresh && _attempted)
            {
                return false;
            }

            _attempted = true;
            _isLoading = true;
            _updateCommandStates();

            var request = new AssetPriceRequestDTO
            {
                Exchange = exchange,
                Ticker = ticker,
                AssetClass = assetClass,
                BrokerName = brokerName,
                Name = name,
                PortfolioName = portfolioName,
                AssetName = assetName
            };

            var price = await priceService.GetCurrentPriceAsync(request);
            if (!string.Equals(_assetKey, assetKey, StringComparison.Ordinal))
            {
                return false;
            }

            var asOf = FormatAsOf(price);
            var snapshot = new TodayInfoSnapshot(price.Price, asOf, price.IsManual);
            _applySnapshot(snapshot);
            _cache[assetKey] = snapshot;
            return true;
        }
        catch (Exception ex)
        {
            if (!string.Equals(_assetKey, assetKey, StringComparison.Ordinal))
            {
                return false;
            }

            setMessage($"Error: {ex.Message}");
            return false;
        }
        finally
        {
            _isLoading = false;
            _updateCommandStates();
            _lock.Release();
        }
    }
}

public sealed record TodayInfoSnapshot(decimal Price, string AsOf, bool IsManual);

