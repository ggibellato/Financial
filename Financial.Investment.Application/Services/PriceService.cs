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

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "SetPrice");
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
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

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "DeletePrice");
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
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
                var result = _assetPriceService.GetCurrentPrice(request);
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "GetCurrentPrice");
                return result;
            }

            try
            {
                var livePrice = _assetPriceService.GetCurrentPrice(request);
                await RecordAutomaticPriceIfNeededAsync(asset, livePrice.Price);
                livePrice.IsManual = false;
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "GetCurrentPrice");
                return livePrice;
            }
            catch
            {
                var fallback = asset.GetPriceForDate(DateOnly.FromDateTime(DateTime.Today));
                if (fallback is null)
                {
                    throw;
                }

                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "GetCurrentPrice");
                return new AssetPriceDTO
                {
                    Exchange = request.Exchange,
                    Ticker = request.Ticker,
                    Name = request.Name ?? string.Empty,
                    Price = fallback.Price,
                    AsOf = null,
                    IsManual = fallback.IsManual
                };
            }
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        var span = _tracer.StartSpan($"Investment.PriceService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "Investment");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }

    private Asset? ResolveAsset(AssetPriceRequestDTO request)
    {
        if (AssetContextValidator.IsInvalid(request.BrokerName, request.PortfolioName, request.AssetName))
        {
            return null;
        }

        return _repository.GetAsset(request.BrokerName, request.PortfolioName, request.AssetName);
    }

    private async Task RecordAutomaticPriceIfNeededAsync(Asset asset, decimal price)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var existing = asset.GetPriceForDate(today);
        var needsWrite = existing is null || existing.IsManual || existing.Price != price;
        if (!needsWrite)
        {
            return;
        }

        asset.SetPrice(today, price, isManual: false);
        await _repository.SaveChangesAsync();
    }
}
