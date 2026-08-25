using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class AssetPriceCrudService : IAssetPriceCrudService
{
    private const string EntityType = "AssetPrice";

    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<AssetPriceCrudService> _logger;

    public AssetPriceCrudService(
        IInvestmentRepository repository,
        INavigationService navigationService,
        ITelemetryTracer tracer,
        ILogger<AssetPriceCrudService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
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

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(AssetPriceCrudService), operationName, EntityType);
    }
}
