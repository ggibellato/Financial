using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.Investment.Infrastructure.Persistence;

public static class InvestmentLoader
{
    // Intentionally synchronous: called from DI factory at startup before the app's async loop begins.
    // ConfigureAwait(false) avoids SynchronizationContext deadlock in WPF startup context.
    public static Investments LoadSync(IJsonStorage storage, IInvestmentSerializer serializer)
    {
        var json = storage.ReadAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        return serializer.Deserialize(json);
    }
}
