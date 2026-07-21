using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;

namespace Financial.Infrastructure.Persistence;

public static class InvestmentsLoader
{
    // Intentionally synchronous: called from DI factory at startup before the app's async loop begins.
    // ConfigureAwait(false) avoids SynchronizationContext deadlock in WPF startup context.
    public static Investments LoadSync(IJsonStorage storage, IInvestmentsSerializer serializer)
    {
        var json = storage.ReadAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        return serializer.Deserialize(json);
    }
}
