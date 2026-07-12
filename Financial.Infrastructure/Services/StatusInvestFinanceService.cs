using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Integrations.WebPageParser;
using Financial.Infrastructure.Interfaces;

namespace Financial.Infrastructure.Services;

public sealed class StatusInvestFinanceService : IFinanceService
{
    public AssetValueSnapshot GetAssetValue(AssetValueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        return StatusInvest.GetSellValue(request.Name);
    }
}
