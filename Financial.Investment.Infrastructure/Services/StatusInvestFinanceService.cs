using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Integrations.WebPageParser;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Services;

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
