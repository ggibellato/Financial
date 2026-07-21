using Financial.Investment.Domain.ValueObjects;

namespace Financial.Investment.Application.Interfaces;

public interface IAssetSnapshotSource
{
    AssetValueSnapshot GetSnapshot(string exchange, string ticker);
}
