using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.CashFlow.Infrastructure.Persistence;

public static class CashFlowLoader
{
    // Intentionally synchronous: called from DI factory at startup before the app's async loop begins.
    public static CashFlowData LoadSync(IJsonStorage storage, ICashFlowSerializer serializer) =>
        ReadSync(storage) is { } json ? serializer.Deserialize(json) : CashFlowData.Create();

    public static string? ReadSync(IJsonStorage storage)
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
