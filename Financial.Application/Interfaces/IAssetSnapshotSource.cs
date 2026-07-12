using Financial.Domain.ValueObjects;

namespace Financial.Application.Interfaces;

public interface IAssetSnapshotSource
{
    AssetValueSnapshot GetSnapshot(string exchange, string ticker);
}
