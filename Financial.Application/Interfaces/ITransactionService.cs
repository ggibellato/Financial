using Financial.Application.DTOs;
using System.Threading.Tasks;

namespace Financial.Application.Interfaces;

public interface ITransactionService
{
    Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request);
    Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request);
    Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request);
}
