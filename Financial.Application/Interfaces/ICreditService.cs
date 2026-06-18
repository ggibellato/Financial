using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface ICreditService
{
    Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request);
    Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request);
    Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request);
}
