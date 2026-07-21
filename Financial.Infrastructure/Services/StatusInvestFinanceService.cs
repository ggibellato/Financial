using Financial.Domain.ValueObjects;
using Financial.Infrastructure.DTOs;
using Financial.Infrastructure.Integrations.WebPageParser;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

public sealed class StatusInvestFinanceService : IFinanceService
{
    private readonly Func<string, AssetValueSnapshot> _lookup;

    public StatusInvestFinanceService() : this(StatusInvest.GetSellValue)
    {
    }

    internal StatusInvestFinanceService(Func<string, AssetValueSnapshot> lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        return _lookup(request.Name);
    }
}
