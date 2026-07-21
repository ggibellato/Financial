using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface ITransactionService
{
    Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request);
    Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request);
    Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request);
}
