using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface ICorporateActionService
{
    Task<AssetDetailsDTO?> AddSplitAsync(CorporateActionSplitCreateDTO request);
    Task<AssetDetailsDTO?> UpdateSplitAsync(CorporateActionSplitUpdateDTO request);
    Task<AssetDetailsDTO?> DeleteSplitAsync(CorporateActionDeleteDTO request);
}
