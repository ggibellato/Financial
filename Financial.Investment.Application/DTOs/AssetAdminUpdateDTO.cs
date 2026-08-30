using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class AssetAdminUpdateDTO
{
    public required string Name { get; set; }

    public string ISIN { get; set; } = string.Empty;

    public string Exchange { get; set; } = string.Empty;

    public string Ticker { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CountryCode Country { get; set; } = CountryCode.Unknown;

    public string LocalTypeCode { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GlobalAssetClass Class { get; set; } = GlobalAssetClass.Unknown;
}
