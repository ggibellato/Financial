using System.Globalization;
using System.Text.Json.Nodes;
using Financial.Shared.Abstractions.Currencies.FxRates;

namespace Financial.Shared.Infrastructure.Persistence.FxRates;

public sealed class FxRateSerializerAdapter : IFxRateSerializer
{
    private const string VersionProperty = "Version";
    private const int CurrentVersion = 1;
    private const string DateFormat = "yyyy-MM-dd";

    public string Serialize(IReadOnlyDictionary<DateOnly, FxRateRecord> ratesByDate)
    {
        ArgumentNullException.ThrowIfNull(ratesByDate);

        var ratesByDateNode = new JsonObject();
        foreach (var (date, record) in ratesByDate)
        {
            ratesByDateNode[date.ToString(DateFormat, CultureInfo.InvariantCulture)] = new JsonObject
            {
                ["base"] = record.Base,
                ["rates"] = new JsonObject
                {
                    ["BRL"] = record.BrlRate,
                    ["GBP"] = record.GbpRate
                },
                ["source"] = record.Source,
                ["storedAt"] = record.StoredAt.ToString("O", CultureInfo.InvariantCulture)
            };
        }

        var root = new JsonObject
        {
            [VersionProperty] = CurrentVersion,
            ["ratesByDate"] = ratesByDateNode
        };

        return root.ToJsonString();
    }

    public Dictionary<DateOnly, FxRateRecord> Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        var result = new Dictionary<DateOnly, FxRateRecord>();
        var root = JsonNode.Parse(json)!.AsObject();

        if (root["ratesByDate"] is not JsonObject ratesByDateNode)
        {
            return result;
        }

        foreach (var (key, value) in ratesByDateNode)
        {
            if (value is not JsonObject entry)
            {
                continue;
            }

            var date = DateOnly.ParseExact(key, DateFormat, CultureInfo.InvariantCulture);
            var rates = entry["rates"]!.AsObject();

            result[date] = new FxRateRecord(
                Base: (string)entry["base"]!,
                BrlRate: (decimal)rates["BRL"]!,
                GbpRate: (decimal)rates["GBP"]!,
                Source: (string)entry["source"]!,
                StoredAt: DateTimeOffset.Parse(
                    (string)entry["storedAt"]!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        }

        return result;
    }
}
