using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

public sealed class TesouroDiretoFinanceService : IFinanceService
{
    private readonly Func<string, AssetValueSnapshot?> _lookup;

    public TesouroDiretoFinanceService() : this(TesouroDireto.GetRedemptionValue)
    {
    }

    internal TesouroDiretoFinanceService(Func<string, AssetValueSnapshot?> lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        return _lookup(request.Name)
            ?? throw new AssetValueNotFoundException($"No Tesouro Direto bond found matching '{request.Name}'.");
    }
}
