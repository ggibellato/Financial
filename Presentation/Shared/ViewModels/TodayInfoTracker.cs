using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Presentation.Shared.ViewModels;

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

    public async Task RefreshAsync(
        bool forceRefresh,
        bool hasAssetContext,
        IAssetPriceService? assetPriceService,
        string exchange,
        string ticker,
        Action<string> setMessage)
    {
        if (!hasAssetContext)
        {
            setMessage("Select an asset to load current values.");
            return;
        }

        if (assetPriceService == null)
        {
            setMessage("Current value service is not available.");
            return;
        }

        if (string.IsNullOrWhiteSpace(exchange) || string.IsNullOrWhiteSpace(ticker))
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
                Ticker = ticker
            };

            var price = await Task.Run(() => assetPriceService.GetCurrentPrice(request));
            if (!string.Equals(_assetKey, assetKey, StringComparison.Ordinal))
            {
                return;
            }

            var asOf = price.AsOf?.ToLocalTime().ToString("g") ?? string.Empty;
            var snapshot = new TodayInfoSnapshot(price.Price, asOf);
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

public sealed record TodayInfoSnapshot(decimal Price, string AsOf);
