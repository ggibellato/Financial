using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Tests;

internal sealed class FakeFinanceService : IFinanceService
{
    private readonly Func<AssetValueRequestDTO, AssetValueSnapshot> _behavior;

    public FakeFinanceService(Func<AssetValueRequestDTO, AssetValueSnapshot> behavior)
    {
        _behavior = behavior;
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request) => _behavior(request);
}
