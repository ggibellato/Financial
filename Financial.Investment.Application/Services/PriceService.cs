using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class PriceService : IPriceService
{
    private const string EntityType = "AssetPrice";

    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly IAssetPriceService _assetPriceService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<PriceService> _logger;

    public PriceService(
        IInvestmentRepository repository,
        INavigationService navigationService,
        IAssetPriceService assetPriceService,
        ITelemetryTracer tracer, ILogger<PriceService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request)
    {
        using var span = StartSpan("SetPrice");
        try
        {
            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset =>
                {
                    asset.SetPrice(request.Date, request.Price, isManual: true);
                    return true;
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "SetPrice");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request)
    {
        using var span = StartSpan("DeletePrice");
        try
        {
            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset =>
                {
                    var existing = asset.GetPriceForDate(request.Date);
                    if (existing is null)
                    {
                        return true;
                    }

                    if (!existing.IsManual)
                    {
                        throw new ArgumentException("Automatic price entries can't be edited directly — add a manual entry for this date instead.");
                    }

                    return asset.RemovePrice(request.Date);
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeletePrice");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request)
    {
        using var span = StartSpan("GetCurrentPrice");
        try
        {
            var asset = ResolveAsset(request);
            if (asset is null)
            {
                WarnWhenAssetContextWasSuppliedButUnresolved(request);
                return CompleteSuccessfully(span, _assetPriceService.GetCurrentPrice(request));
            }

            // A manual price for today is authoritative: the rest of the app refuses to edit or
            // delete an automatic entry and tells the user to add a manual one to override, so a
            // scrape must not overwrite it. No fetch is made at all, since its result would be
            // discarded. Removing the manual entry restores automatic pricing.
            var manualPriceForToday = FindManualPriceForToday(asset);
            if (manualPriceForToday is not null)
            {
                return CompleteSuccessfully(span, BuildPriceFrom(manualPriceForToday, request));
            }

            var (price, wasFetchedLive) = FetchWithPriceHistoryFallback(asset, request);
            if (!wasFetchedLive)
            {
                return CompleteSuccessfully(span, price);
            }

            // Recording sits outside the fetch's exception handling on purpose. When it was
            // inside, a persistence failure fell into the fallback branch, which found the entry
            // that SetPrice had just added in memory, and so returned a successful response for a
            // write that never happened.
            var persistenceError = await TryRecordAutomaticPriceAsync(asset, price.Price, request);
            if (persistenceError is null)
            {
                return CompleteSuccessfully(span, price);
            }

            // The price is still returned so a storage fault does not block the caller from
            // seeing a live value, but the operation is reported as failed rather than claiming
            // a record was made.
            span.MarkFailed(persistenceError);
            return price;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private AssetPriceDTO CompleteSuccessfully(ITelemetrySpan span, AssetPriceDTO price)
    {
        span.MarkSuccess();
        _logger.LogInformation("{Operation} completed", "GetCurrentPrice");
        return price;
    }

    private (AssetPriceDTO Price, bool WasFetchedLive) FetchWithPriceHistoryFallback(
        Asset asset,
        AssetPriceRequestDTO request)
    {
        try
        {
            var livePrice = _assetPriceService.GetCurrentPrice(request);
            livePrice.IsManual = false;
            return (livePrice, true);
        }
        catch
        {
            var fallback = asset.GetPriceForDate(DateOnly.FromDateTime(DateTime.Today));
            if (fallback is null)
            {
                throw;
            }

            return (BuildPriceFrom(fallback, request), false);
        }
    }

    /// <summary>
    /// Returns the exception that prevented the price being recorded, or null when it was
    /// recorded. A persistence failure is reported rather than thrown, so the caller still
    /// receives the price it asked for.
    /// </summary>
    private async Task<Exception?> TryRecordAutomaticPriceAsync(
        Asset asset,
        decimal price,
        AssetPriceRequestDTO request)
    {
        try
        {
            await RecordAutomaticPriceIfNeededAsync(asset, price);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Automatic price for {BrokerName}/{PortfolioName}/{AssetName} could not be persisted",
                request.BrokerName,
                request.PortfolioName,
                request.AssetName);
            return ex;
        }
    }

    /// <summary>
    /// A blank broker, portfolio, or asset name is a deliberate lookup-only request. All three
    /// supplied but matching nothing is a misrouted request that would otherwise return a price
    /// and record nothing, with no trace of why.
    /// </summary>
    private void WarnWhenAssetContextWasSuppliedButUnresolved(AssetPriceRequestDTO request)
    {
        if (AssetContextValidator.IsInvalid(request.BrokerName, request.PortfolioName, request.AssetName))
        {
            return;
        }

        _logger.LogWarning(
            "Price returned without recording: no asset matches {BrokerName}/{PortfolioName}/{AssetName}",
            request.BrokerName,
            request.PortfolioName,
            request.AssetName);
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(PriceService), operationName, EntityType);
    }

    private Asset? ResolveAsset(AssetPriceRequestDTO request)
    {
        if (AssetContextValidator.IsInvalid(request.BrokerName, request.PortfolioName, request.AssetName))
        {
            return null;
        }

        return _repository.GetAsset(request.BrokerName, request.PortfolioName, request.AssetName);
    }

    private static AssetPriceSnapshot? FindManualPriceForToday(Asset asset)
    {
        var entry = asset.GetPriceForDate(DateOnly.FromDateTime(DateTime.Today));
        return entry?.IsManual == true ? entry : null;
    }

    /// <summary>
    /// A stored entry carries a date but no time of day, so AsOf stays null here rather than
    /// inventing midnight.
    /// </summary>
    private static AssetPriceDTO BuildPriceFrom(AssetPriceSnapshot snapshot, AssetPriceRequestDTO request) =>
        new()
        {
            Exchange = request.Exchange,
            Ticker = request.Ticker,
            Name = request.Name ?? string.Empty,
            Price = snapshot.Price,
            AsOf = null,
            IsManual = snapshot.IsManual
        };

    private async Task RecordAutomaticPriceIfNeededAsync(Asset asset, decimal price)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var existing = asset.GetPriceForDate(today);

        // A manual entry is never overwritten. GetCurrentPriceAsync returns before reaching this
        // point when one exists, so this is the second line of defence rather than the first.
        var needsWrite = existing is null || (!existing.IsManual && existing.Price != price);
        if (!needsWrite)
        {
            return;
        }

        asset.SetPrice(today, price, isManual: false);
        await _repository.SaveChangesAsync();
    }
}
