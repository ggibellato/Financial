using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface ICreditService
{
    Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request);
    Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request);
    Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request);
}
