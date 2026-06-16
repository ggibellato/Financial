using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface ITransactionService
{
    AssetDetailsDTO? AddTransaction(TransactionCreateDTO request);
    AssetDetailsDTO? UpdateTransaction(TransactionUpdateDTO request);
    AssetDetailsDTO? DeleteTransaction(TransactionDeleteDTO request);
}
