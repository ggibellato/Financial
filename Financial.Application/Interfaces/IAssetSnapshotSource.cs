using Financial.Domain.Entities;

namespace Financial.Application.Interfaces;

public interface IAssetSnapshotSource
{
    AssetValueSnapshot GetSnapshot(string exchange, string ticker);
}
