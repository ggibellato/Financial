using Financial.Application.Interfaces;
using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;

namespace Financial.Infrastructure.Services;

public sealed class AssetSnapshotSourceAdapter : IAssetSnapshotSource
{
    public AssetValueSnapshot GetSnapshot(string exchange, string ticker) =>
        GoogleFinance.GetFinancialInfoSnapshot(exchange, ticker);
}
