using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

public sealed class CreditService : ICreditService
{
    private readonly IRepository _repository;
    private readonly INavigationService _navigationService;

    public CreditService(IRepository repository, INavigationService navigationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request)
    {
        return AssetServiceHelper.ExecuteParsedMutationAsync<Credit.CreditType>(
            _repository,
            _navigationService,
            request.BrokerName,
            request.PortfolioName,
            request.AssetName,
            request.Type,
            CreditTypeParser.TryParse,
            (asset, creditType) =>
            {
                var credit = Credit.Create(request.Date, creditType, request.Value);
                asset.AddCredit(credit);
                return true;
            });
    }

    public Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request)
    {
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult<AssetDetailsDTO?>(null);
        }

        return AssetServiceHelper.ExecuteParsedMutationAsync<Credit.CreditType>(
            _repository,
            _navigationService,
            request.BrokerName,
            request.PortfolioName,
            request.AssetName,
            request.Type,
            CreditTypeParser.TryParse,
            (asset, creditType) =>
            {
                var updatedCredit = Credit.CreateWithId(request.Id, request.Date, creditType, request.Value);
                return asset.UpdateCredit(updatedCredit);
            });
    }

    public Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request)
    {
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult<AssetDetailsDTO?>(null);
        }

        return AssetServiceHelper.ExecuteAssetMutationAsync(
            _repository,
            _navigationService,
            request.BrokerName,
            request.PortfolioName,
            request.AssetName,
            asset => asset.RemoveCredit(request.Id));
    }
}
