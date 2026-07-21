using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Tests;

internal sealed class StubFinanceService : IFinanceService
{
    private readonly AssetValueSnapshot? _snapshot;

    public StubFinanceService(AssetValueSnapshot? snapshot = null)
    {
        _snapshot = snapshot;
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request) => _snapshot ?? throw new NotImplementedException();
}
