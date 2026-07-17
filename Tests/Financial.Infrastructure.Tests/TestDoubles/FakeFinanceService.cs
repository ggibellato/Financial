using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Tests;

internal sealed class FakeFinanceService : IFinanceService
{
    private readonly AssetValueSnapshot? _snapshot;

    public FakeFinanceService(AssetValueSnapshot? snapshot = null)
    {
        _snapshot = snapshot;
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequest request) => _snapshot ?? throw new NotImplementedException();
}
