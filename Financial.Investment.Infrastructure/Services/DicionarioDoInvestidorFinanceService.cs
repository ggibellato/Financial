using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Integrations.WebPageParser;
using Financial.Investment.Infrastructure.Interfaces;

namespace Financial.Investment.Infrastructure.Services;

public sealed class DicionarioDoInvestidorFinanceService : IFinanceService
{
    private readonly Func<string, AssetValueSnapshot> _lookup;

    public DicionarioDoInvestidorFinanceService()
        : this(name => WebPageParserMappers.ToAssetValueSnapshot(DicionarioDoInvestidor.GetSellValue(name), PriceSource.DicionarioDoInvestidor))
    {
    }

    internal DicionarioDoInvestidorFinanceService(Func<string, AssetValueSnapshot> lookup)
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
