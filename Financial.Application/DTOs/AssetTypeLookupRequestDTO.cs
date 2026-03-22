using Financial.Domain.Entities;

namespace Financial.Application.DTOs;

public sealed class AssetTypeLookupRequestDTO
{
    public string Name { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public string ISIN { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public CountryCode FallbackCountry { get; set; } = CountryCode.Unknown;
}
