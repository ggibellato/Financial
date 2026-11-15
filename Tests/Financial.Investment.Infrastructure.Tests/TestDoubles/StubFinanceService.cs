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

    public AssetValueRequestDTO? LastRequest { get; private set; }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request)
    {
        LastRequest = request;
        return _snapshot ?? throw new NotImplementedException();
    }
}
