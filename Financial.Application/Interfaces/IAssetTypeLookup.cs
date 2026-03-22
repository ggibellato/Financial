using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IAssetTypeLookup
{
    Task<AssetTypeLookupResultDTO> LookupAsync(AssetTypeLookupRequestDTO request);
}
