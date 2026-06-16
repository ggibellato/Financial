using Financial.Application.DTOs;
using System.Threading.Tasks;

namespace Financial.Application.Interfaces;

public interface ICreditService
{
    Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request);
    Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request);
    Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request);
}
