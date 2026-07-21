using Financial.Domain.ValueObjects;
using Financial.Infrastructure.DTOs;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Tests;

internal sealed class StubFinanceService : IFinanceService
{
    private readonly AssetValueSnapshot? _snapshot;

    public StubFinanceService(AssetValueSnapshot? snapshot = null)
    {
        _snapshot = snapshot;
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request) => _snapshot ?? throw new NotImplementedException();
}
