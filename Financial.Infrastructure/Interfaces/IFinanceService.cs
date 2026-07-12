using Financial.Domain.ValueObjects;

namespace Financial.Infrastructure.Interfaces;

public interface IFinanceService
{
    AssetValueSnapshot GetAssetValue(AssetValueRequest request);
}
