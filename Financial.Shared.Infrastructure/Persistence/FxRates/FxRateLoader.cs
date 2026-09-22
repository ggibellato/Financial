using System.Diagnostics;
using System.Text.Json;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.Shared.Infrastructure.Persistence.FxRates;

public static class FxRateLoader
{
    // Intentionally synchronous: called from DI factory at startup before the app's async loop begins.
    public static Dictionary<DateOnly, FxRateRecord> LoadSync(IJsonStorage storage, IFxRateSerializer serializer)
    {
        string json;
        try
        {
            json = storage.ReadAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (FileNotFoundException)
        {
            return new Dictionary<DateOnly, FxRateRecord>();
        }

        try
        {
            return serializer.Deserialize(json);
        }
        catch (JsonException ex)
        {
            Trace.TraceWarning(
                $"FxRateLoader: data-fx-rates.json contains invalid JSON ({ex.GetType().Name}); starting with an empty FX rate store.");
            return new Dictionary<DateOnly, FxRateRecord>();
        }
    }
}
