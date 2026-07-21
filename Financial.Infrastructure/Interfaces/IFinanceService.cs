using Financial.Investment.Domain.ValueObjects;
using Financial.Infrastructure.DTOs;

namespace Financial.Infrastructure.Interfaces;

public interface IFinanceService
{
    AssetValueSnapshot GetAssetValue(AssetValueRequestDTO request);
}
