using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Infrastructure.Persistence;

namespace Financial.CashFlow.Infrastructure.Persistence;

public static class CashFlowLoader
{
    // Intentionally synchronous: called from DI factory at startup before the app's async loop begins.
    // ConfigureAwait(false) avoids SynchronizationContext deadlock in WPF startup context.
    public static CashFlowData LoadSync(IJsonStorage storage, ICashFlowSerializer serializer)
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
            return CashFlowData.Create();
        }

        return serializer.Deserialize(json);
    }
}
