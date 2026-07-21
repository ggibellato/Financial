using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;

namespace Financial.Infrastructure.Services;

public sealed class AssetSnapshotSourceAdapter : IAssetSnapshotSource
{
    private readonly Func<string, string, AssetValueSnapshot> _lookup;

    public AssetSnapshotSourceAdapter() : this(GoogleFinance.GetFinancialInfoSnapshot)
    {
    }

    internal AssetSnapshotSourceAdapter(Func<string, string, AssetValueSnapshot> lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public AssetValueSnapshot GetSnapshot(string exchange, string ticker) =>
        _lookup(exchange, ticker);
}
