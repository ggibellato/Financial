using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using System;

namespace Financial.Infrastructure.Repositories;

public sealed class OperationService : IOperationService
{
    private readonly IRepository _repository;
    private readonly INavigationService _navigationService;

    public OperationService(IRepository repository, INavigationService navigationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public AssetDetailsDTO? AddOperation(OperationCreateDTO request)
    {
        if (IsInvalidContext(request.BrokerName, request.PortfolioName, request.AssetName) ||
            !TryParseOperationType(request.Type, out var operationType))
        {
            return null;
        }

        var asset = _repository.GetAsset(request.BrokerName, request.PortfolioName, request.AssetName);
        if (asset == null)
        {
            return null;
        }

        var operation = Operation.Create(request.Date, operationType, request.Quantity, request.UnitPrice, request.Fees);
        asset.AddOperation(operation);
        _repository.SaveChanges();

        return _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, request.AssetName);
    }

    public AssetDetailsDTO? UpdateOperation(OperationUpdateDTO request)
    {
        if (IsInvalidContext(request.BrokerName, request.PortfolioName, request.AssetName) ||
            request.Id == Guid.Empty ||
            !TryParseOperationType(request.Type, out var operationType))
        {
            return null;
        }

        var asset = _repository.GetAsset(request.BrokerName, request.PortfolioName, request.AssetName);
        if (asset == null)
        {
            return null;
        }

        var updatedOperation = Operation.CreateWithId(request.Id, request.Date, operationType, request.Quantity, request.UnitPrice, request.Fees);
        if (!asset.UpdateOperation(updatedOperation))
        {
            return null;
        }

        _repository.SaveChanges();
        return _navigationService.GetAssetDetails(request.BrokerName, request.PortfolioName, request.AssetName);
    }

    public AssetDetailsDTO? DeleteOperation(OperationDeleteDTO request)
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

        if (!asset.RemoveOperation(request.Id))
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

    private static bool TryParseOperationType(string? value, out Operation.OperationType operationType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            operationType = default;
            return false;
        }

        return Enum.TryParse(value, true, out operationType);
    }
}
