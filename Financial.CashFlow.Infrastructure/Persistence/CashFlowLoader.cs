using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.CashFlow.Infrastructure.Persistence;

public static class CashFlowLoader
{
    // Intentionally synchronous: called from DI factory at startup before the app's async loop begins.
    public static CashFlowData LoadSync(IJsonStorage storage, ICashFlowSerializer serializer) =>
        LoadWithPersistedJsonSync(storage, serializer).Data;

    public static (CashFlowData Data, string? PersistedJson) LoadWithPersistedJsonSync(IJsonStorage storage, ICashFlowSerializer serializer)
    {
        var persistedJson = ReadSync(storage);
        return (persistedJson is null ? CashFlowData.Create() : serializer.Deserialize(persistedJson), persistedJson);
    }

    private static string? ReadSync(IJsonStorage storage)
    {
        try
        {
            return storage.ReadAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
