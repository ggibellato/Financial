using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;
using Financial.Domain.Entities;
using System;

namespace Financial.Infrastructure.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly IRepository _repository;
    private readonly INavigationService _navigationService;

    public TransactionService(IRepository repository, INavigationService navigationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public AssetDetailsDTO? AddTransaction(TransactionCreateDTO request)
    {
        return AssetServiceHelper.ExecuteParsedMutation<Transaction.TransactionType>(
            _repository,
            _navigationService,
            request.BrokerName,
            request.PortfolioName,
            request.AssetName,
            request.Type,
            TransactionTypeParser.TryParse,
            (asset, transactionType) =>
            {
                var transaction = Transaction.Create(request.Date, transactionType, request.Quantity, request.UnitPrice, request.Fees);
                asset.AddTransaction(transaction);
                return true;
            });
    }

    public AssetDetailsDTO? UpdateTransaction(TransactionUpdateDTO request)
    {
        if (request.Id == Guid.Empty)
        {
            return null;
        }

        return AssetServiceHelper.ExecuteParsedMutation<Transaction.TransactionType>(
            _repository,
            _navigationService,
            request.BrokerName,
            request.PortfolioName,
            request.AssetName,
            request.Type,
            TransactionTypeParser.TryParse,
            (asset, transactionType) =>
            {
                var updatedTransaction = Transaction.CreateWithId(request.Id, request.Date, transactionType, request.Quantity, request.UnitPrice, request.Fees);
                return asset.UpdateTransaction(updatedTransaction);
            });
    }

    public AssetDetailsDTO? DeleteTransaction(TransactionDeleteDTO request)
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
            asset => asset.RemoveTransaction(request.Id));
    }
}
