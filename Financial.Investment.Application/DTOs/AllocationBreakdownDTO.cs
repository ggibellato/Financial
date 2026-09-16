using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public sealed record AssetClassAllocationEntryDTO(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GlobalAssetClass Class,
    decimal MarketValue, decimal Percentage);

public sealed record CurrencyAllocationEntryDTO(string Currency, decimal MarketValue, decimal Percentage);

public sealed record CountryAllocationEntryDTO(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CountryCode Country,
    decimal MarketValue, decimal Percentage);

public sealed record BrokerAllocationEntryDTO(string BrokerName, decimal MarketValue, decimal Percentage);

public sealed class AllocationBreakdownDTO
{
    public IReadOnlyList<AssetClassAllocationEntryDTO> ByClass { get; init; } = [];
    public IReadOnlyList<CurrencyAllocationEntryDTO> ByCurrency { get; init; } = [];
    public IReadOnlyList<CountryAllocationEntryDTO> ByCountry { get; init; } = [];
    public IReadOnlyList<BrokerAllocationEntryDTO> ByBroker { get; init; } = [];
}
