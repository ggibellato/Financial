using System.Text.Json;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace Financial.Shared.Infrastructure.Persistence.FxRates;

public static class FxRateLoader
{
    // Intentionally synchronous: called from DI factory at startup before the app's async loop begins.
    public static Dictionary<DateOnly, FxRateRecord> LoadSync(IJsonStorage storage, IFxRateSerializer serializer, ILogger logger)
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
            logger.LogWarning("{Operation} started with an empty FX rate store after {ErrorType}", "LoadFxRates", ex.GetType().Name);
            return new Dictionary<DateOnly, FxRateRecord>();
        }
    }
}
