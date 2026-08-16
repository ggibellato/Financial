using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Services;

public sealed class PriceService : IPriceService
{
    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly IAssetPriceService _assetPriceService;

    public PriceService(IInvestmentRepository repository, INavigationService navigationService, IAssetPriceService assetPriceService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
    }

    public Task<AssetDetailsDTO?> SetPriceAsync(SetAssetPriceDTO request)
    {
        return AssetMutationHelper.ExecuteAssetMutationAsync(
            _repository,
            _navigationService,
            request.BrokerName,
            request.PortfolioName,
            request.AssetName,
            asset =>
            {
                asset.SetPrice(request.Date, request.Price, isManual: true);
                return true;
            });
    }

    public Task<AssetDetailsDTO?> DeletePriceAsync(DeleteAssetPriceDTO request)
    {
        return AssetMutationHelper.ExecuteAssetMutationAsync(
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
            });
    }

    public async Task<AssetPriceDTO> GetCurrentPriceAsync(AssetPriceRequestDTO request)
    {
        var asset = ResolveAsset(request);
        if (asset is null)
        {
            return _assetPriceService.GetCurrentPrice(request);
        }

        try
        {
            var livePrice = _assetPriceService.GetCurrentPrice(request);
            await RecordAutomaticPriceIfNeededAsync(asset, livePrice.Price);
            livePrice.IsManual = false;
            return livePrice;
        }
        catch
        {
            var fallback = asset.GetPriceForDate(DateOnly.FromDateTime(DateTime.Today));
            if (fallback is null)
            {
                throw;
            }

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
