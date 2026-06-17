using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Infrastructure.Persistence;

public static class InvestmentsLoader
{
    public static Investments Load(IJsonStorage storage, IInvestmentsSerializer serializer)
    {
        var json = storage.ReadAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        return serializer.Deserialize(json);
    }
}
