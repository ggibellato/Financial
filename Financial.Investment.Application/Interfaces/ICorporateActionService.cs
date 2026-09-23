using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface ICorporateActionService
{
    Task<AssetDetailsDTO?> AddSplitAsync(CorporateActionSplitCreateDTO request);
    Task<AssetDetailsDTO?> UpdateSplitAsync(CorporateActionSplitUpdateDTO request);
    Task<CorporateActionMergerResultDTO?> AddMergerAsync(CorporateActionMergerCreateDTO request);
    Task<CorporateActionMergerResultDTO?> UpdateMergerAsync(CorporateActionMergerUpdateDTO request);
    Task<CorporateActionSpinOffResultDTO?> AddSpinOffAsync(CorporateActionSpinOffCreateDTO request);
    Task<CorporateActionSpinOffResultDTO?> UpdateSpinOffAsync(CorporateActionSpinOffUpdateDTO request);
    Task<AssetDetailsDTO?> DeleteCorporateActionAsync(CorporateActionDeleteDTO request);
}
