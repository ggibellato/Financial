using Financial.Shared.Abstractions.Currencies.FxRates;

namespace Financial.Shared.Infrastructure.Persistence.FxRates;

public interface IFxRateSerializer
{
    string Serialize(IReadOnlyDictionary<DateOnly, FxRateRecord> ratesByDate);

    Dictionary<DateOnly, FxRateRecord> Deserialize(string json);
}
