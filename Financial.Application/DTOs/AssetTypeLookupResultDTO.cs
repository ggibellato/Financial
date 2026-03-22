using Financial.Domain.Entities;

namespace Financial.Application.DTOs;

public sealed class AssetTypeLookupResultDTO
{
    public CountryCode Country { get; set; } = CountryCode.Unknown;
    public string LocalTypeCode { get; set; } = string.Empty;
}
