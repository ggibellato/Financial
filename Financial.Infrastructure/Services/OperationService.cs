using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;
using Financial.Domain.Entities;
using System;

namespace Financial.Infrastructure.Services;

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
        if (!OperationTypeParser.TryParse(request.Type, out Operation.OperationType operationType))
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
                var operation = Operation.Create(request.Date, operationType, request.Quantity, request.UnitPrice, request.Fees);
                asset.AddOperation(operation);
                return true;
            });
    }

    public AssetDetailsDTO? UpdateOperation(OperationUpdateDTO request)
    {
        if (request.Id == Guid.Empty ||
            !OperationTypeParser.TryParse(request.Type, out Operation.OperationType operationType))
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
                var updatedOperation = Operation.CreateWithId(request.Id, request.Date, operationType, request.Quantity, request.UnitPrice, request.Fees);
                return asset.UpdateOperation(updatedOperation);
            });
    }

    public AssetDetailsDTO? DeleteOperation(OperationDeleteDTO request)
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
            asset => asset.RemoveOperation(request.Id));
    }
}

