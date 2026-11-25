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

    /// <summary>
    /// Left null to re-derive from Country/LocalTypeCode via <see cref="Domain.Rules.GlobalAssetClassMapping"/>,
    /// matching <see cref="AssetAdminCreateDTO.Class"/>'s convention; set to override explicitly.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GlobalAssetClass? Class { get; set; }

    /// <summary>Left null to default to <see cref="ValuationMethod.Unspecified"/> (today's asset-class-based routing).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ValuationMethod? ValuationMethod { get; set; }

    /// <summary>Left null to default to <see cref="IncomePolicy.Unknown"/>.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IncomePolicy? IncomePolicy { get; set; }

    /// <summary>Unlike the fields above, null or blank clears the segment rather than keeping it.</summary>
    public string? Segment { get; set; }

    /// <summary>"Recommended" or "MyChoice"; unlike the fields above, null or blank clears it rather than keeping it.</summary>
    public string? PickSource { get; set; }
}
