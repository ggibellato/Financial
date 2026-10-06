using System.IO;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;

namespace Financial.Investment.Infrastructure.Tests.Services;

internal sealed class PersistedInvestmentFile : IDisposable
{
    private readonly InvestmentSerializerAdapter _serializer = new();

    public PersistedInvestmentFile()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
        File.Copy(TestDataPaths.DataJsonFile, Path, true);
    }

    public string Path { get; }

    public InvestmentJsonRepository OpenRepository()
    {
        var storage = new LocalJsonStorage(Path);
        return new InvestmentJsonRepository(InvestmentLoader.LoadSync(storage, _serializer, TestClock.At()), storage, _serializer);
    }

    public Asset ReloadAsset(string broker, string portfolio, string asset) =>
        OpenRepository().GetInvestments().ActiveBrokers
            .Single(b => b.Name == broker).Portfolios
            .Single(p => p.Name == portfolio).Assets
            .Single(a => a.Name == asset);

    public void Dispose() => File.Delete(Path);
}
