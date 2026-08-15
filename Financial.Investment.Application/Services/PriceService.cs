using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;

namespace Financial.Investment.Application.Services;

public sealed class PriceService : IPriceService
{
    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;

    public PriceService(IInvestmentRepository repository, INavigationService navigationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
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
}
