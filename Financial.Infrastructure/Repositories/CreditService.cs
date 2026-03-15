using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;
using Financial.Domain.Entities;
using System;

namespace Financial.Infrastructure.Repositories;

public sealed class CreditService : ICreditService
{
    private readonly IRepository _repository;
    private readonly INavigationService _navigationService;

    public CreditService(IRepository repository, INavigationService navigationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public AssetDetailsDTO? AddCredit(CreditCreateDTO request)
    {
        if (!CreditTypeParser.TryParse(request.Type, out Credit.CreditType creditType))
        {
            return null;
        }

        return AssetServiceHelper.ExecuteAssetMutation(
            _repository,
            _navigationService,
            request.BrokerName,
            request.PortfolioName,
            request.AssetName,
            asset =>
            {
                var credit = Credit.Create(request.Date, creditType, request.Value);
                asset.AddCredit(credit);
                return true;
            });
    }

    public AssetDetailsDTO? UpdateCredit(CreditUpdateDTO request)
    {
        if (request.Id == Guid.Empty ||
            !CreditTypeParser.TryParse(request.Type, out Credit.CreditType creditType))
        {
            return null;
        }

        return AssetServiceHelper.ExecuteAssetMutation(
            _repository,
            _navigationService,
            request.BrokerName,
            request.PortfolioName,
            request.AssetName,
            asset =>
            {
                var updatedCredit = Credit.CreateWithId(request.Id, request.Date, creditType, request.Value);
                return asset.UpdateCredit(updatedCredit);
            });
    }

    public AssetDetailsDTO? DeleteCredit(CreditDeleteDTO request)
    {
        if (request.Id == Guid.Empty)
        {
            return null;
        }

        return AssetServiceHelper.ExecuteAssetMutation(
            _repository,
            _navigationService,
            request.BrokerName,
            request.PortfolioName,
            request.AssetName,
            asset => asset.RemoveCredit(request.Id));
    }
}
