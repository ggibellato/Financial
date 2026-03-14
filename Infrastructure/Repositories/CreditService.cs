using Financial.Application.DTOs;
using Financial.Application.Interfaces;
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
        if (IsInvalidContext(request.BrokerName, request.PortfolioName, request.AssetName) ||
            !TryParseCreditType(request.Type, out var creditType))
        {
            return null;
        }

        var asset = _repository.GetAsset(request.BrokerName, request.PortfolioName, request.AssetName);
        if (asset == null)
        {
            return null;
        }

        var credit = Credit.Create(request.Date, creditType, request.Value);
        asset.AddCredit(credit);
        _repository.SaveChanges();

        return _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, request.AssetName);
    }

    public AssetDetailsDTO? UpdateCredit(CreditUpdateDTO request)
    {
        if (IsInvalidContext(request.BrokerName, request.PortfolioName, request.AssetName) ||
            request.Id == Guid.Empty ||
            !TryParseCreditType(request.Type, out var creditType))
        {
            return null;
        }

        var asset = _repository.GetAsset(request.BrokerName, request.PortfolioName, request.AssetName);
        if (asset == null)
        {
            return null;
        }

        var updatedCredit = Credit.CreateWithId(request.Id, request.Date, creditType, request.Value);
        if (!asset.UpdateCredit(updatedCredit))
        {
            return null;
        }

        _repository.SaveChanges();
        return _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, request.AssetName);
    }

    public AssetDetailsDTO? DeleteCredit(CreditDeleteDTO request)
    {
        if (IsInvalidContext(request.BrokerName, request.PortfolioName, request.AssetName) ||
            request.Id == Guid.Empty)
        {
            return null;
        }

        var asset = _repository.GetAsset(request.BrokerName, request.PortfolioName, request.AssetName);
        if (asset == null)
        {
            return null;
        }

        if (!asset.RemoveCredit(request.Id))
        {
            return null;
        }

        _repository.SaveChanges();
        return _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, request.AssetName);
    }

    private static bool IsInvalidContext(string? brokerName, string? portfolioName, string? assetName)
    {
        return string.IsNullOrWhiteSpace(brokerName) ||
               string.IsNullOrWhiteSpace(portfolioName) ||
               string.IsNullOrWhiteSpace(assetName);
    }

    private static bool TryParseCreditType(string? value, out Credit.CreditType creditType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            creditType = default;
            return false;
        }

        return Enum.TryParse(value, true, out creditType);
    }
}
