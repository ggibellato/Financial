using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;
using Financial.Domain.Entities;
using System;
using System.Threading.Tasks;

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

    public Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request)
    {
        return AssetServiceHelper.ExecuteParsedMutationAsync<Transaction.TransactionType>(
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

    public Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request)
    {
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult<AssetDetailsDTO?>(null);
        }

        return AssetServiceHelper.ExecuteParsedMutationAsync<Transaction.TransactionType>(
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

    public Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request)
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
            asset => asset.RemoveTransaction(request.Id));
    }
}
