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
    /// a date, so it is shown as one rather than padded with a midnight that was never measured.</summary>
    private static string FormatAsOf(AssetPriceDTO price) =>
        price.AsOf?.ToLocalTime().ToString("g")
        ?? price.AsOfDate?.ToString("d")
        ?? string.Empty;

    public async Task RefreshAsync(
        bool forceRefresh,
        bool hasAssetContext,
        IPriceService? priceService,
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
            return;
        }

        if (priceService == null)
        {
            setMessage("Current value service is not available.");
            return;
        }

        var isCryptocurrency = assetClass == GlobalAssetClass.Cryptocurrency;
        var isBond = assetClass == GlobalAssetClass.Bond;

        if (string.IsNullOrWhiteSpace(ticker))
        {
            setMessage("Asset exchange or ticker is missing.");
            return;
        }

        if (isBond)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                setMessage("Asset name is missing.");
                return;
            }
        }
        else if (!isCryptocurrency && string.IsNullOrWhiteSpace(exchange))
        {
            setMessage("Asset exchange or ticker is missing.");
            return;
        }

        await _lock.WaitAsync();
        var assetKey = _assetKey;
        try
        {
            if (!forceRefresh && _attempted)
            {
                return;
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
                return;
            }

            var asOf = FormatAsOf(price);
            var snapshot = new TodayInfoSnapshot(price.Price, asOf, price.IsManual);
            _applySnapshot(snapshot);
            _cache[assetKey] = snapshot;
        }
        catch (Exception ex)
        {
            if (!string.Equals(_assetKey, assetKey, StringComparison.Ordinal))
            {
                return;
            }

            setMessage($"Error: {ex.Message}");
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

